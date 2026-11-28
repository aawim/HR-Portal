using System.Runtime.CompilerServices;
using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Models;
using HRM.AuditLogs.Options;
using HRM.AuditLogs.Persistence;
using HRM.AuditLogs.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AuditLogs.Interceptors;

public sealed class AuditSaveChangesInterceptor(
    IAuditLogService auditLogService,
    IAuditContextAccessor contextAccessor,
    IAuditClock clock,
    IOptions<AuditLogOptions> optionsAccessor,
    ILogger<AuditSaveChangesInterceptor> logger) : SaveChangesInterceptor
{
    private readonly AuditLogOptions _options = optionsAccessor.Value;
    private readonly ConditionalWeakTable<DbContext, PendingAuditBatch> _pending = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            CapturePendingChanges(eventData.Context, asyncMode: false, CancellationToken.None).GetAwaiter().GetResult();
        }
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await CapturePendingChanges(eventData.Context, asyncMode: true, cancellationToken);
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
        {
            PersistPendingAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        }
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await PersistPendingAsync(eventData.Context, cancellationToken);
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is not null)
        {
            CleanupFailedAsync(eventData.Context).GetAwaiter().GetResult();
        }
        base.SaveChangesFailed(eventData);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await CleanupFailedAsync(eventData.Context);
        }
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private async Task CapturePendingChanges(DbContext context, bool asyncMode, CancellationToken cancellationToken)
    {
        if (!_options.EnableAutomaticAuditing || context is AuditDbContext)
            return;

        // Remove any stale batch from an earlier aborted attempt on this context
        _pending.Remove(context);

        var actor = asyncMode
            ? await contextAccessor.GetCurrentContextAsync(cancellationToken)
            : (contextAccessor.GetCurrentContext() ?? AuditActorContext.System(_options.DefaultSource));

        var nowUtc = clock.UtcNow;
        var items = new List<PendingAuditItem>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (entry.Entity is AuditLog or AuditLogDetail)
                continue;

            var entityName = entry.Metadata.ClrType.Name;
            if (!_options.Policy.ShouldAuditEntity(entityName, out var rule))
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "EVENT"
            };

            var changes = new List<AuditPropertyChange>();
            var keyProperties = new List<PropertyEntry>();

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsPrimaryKey())
                    keyProperties.Add(prop);

                if (prop.Metadata.IsShadowProperty())
                    continue;

                var propName = prop.Metadata.Name;
                if (_options.Policy.GlobalExcludedProperties.Contains(propName) ||
                    (rule != null && rule.ExcludedProperties.Contains(propName)))
                    continue;

                if (entry.State == EntityState.Added)
                {
                    if (prop.CurrentValue is not null)
                    {
                        changes.Add(new AuditPropertyChange(propName, null, prop.CurrentValue, prop.Metadata.ClrType.Name));
                    }
                }
                else if (entry.State == EntityState.Deleted)
                {
                    if (prop.OriginalValue is not null)
                    {
                        changes.Add(new AuditPropertyChange(propName, prop.OriginalValue, null, prop.Metadata.ClrType.Name));
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (!prop.IsModified) continue;
                    if (AuditValueSerializer.AreEqual(prop.OriginalValue, prop.CurrentValue)) continue;

                    changes.Add(new AuditPropertyChange(propName, prop.OriginalValue, prop.CurrentValue, prop.Metadata.ClrType.Name));
                }
            }

            // Do not emit no-op UPDATE records when no tracked auditable fields actually changed.
            if (entry.State == EntityState.Modified && changes.Count == 0)
                continue;

            var orgId = ResolveIntProperty(entry, rule?.OrganisationIdProperty, "OrganisationId", "OrganisationID", "OrganisationBusinessEntityId", "OrganisationBusinessEntityID", "UserOrganisationId")
                ?? actor.OrganisationId;
            var divId = ResolveIntProperty(entry, rule?.DivisionIdProperty, "DivisionId", "OrganisationStructureId")
                ?? actor.DivisionId;

            items.Add(new PendingAuditItem
            {
                EventId = Guid.NewGuid(),
                Action = action,
                Module = rule?.Module ?? "General",
                EntityType = entityName,
                KeyProperties = keyProperties,
                CapturedKey = FormatKey(keyProperties, useOriginal: entry.State == EntityState.Deleted),
                HasTemporaryKey = keyProperties.Any(k => k.IsTemporary),
                IsMandatory = rule?.IsMandatory ?? true,
                OrganisationId = orgId,
                DivisionId = divId,
                OccurredAtUtc = nowUtc,
                Actor = actor,
                Changes = changes
            });
        }

        if (items.Count == 0)
            return;

        IDbContextTransaction? ownedTx = null;
        if (context.Database.IsSqlServer() && context.Database.CurrentTransaction is null)
        {
            ownedTx = asyncMode
                ? await context.Database.BeginTransactionAsync(cancellationToken)
                : context.Database.BeginTransaction();
        }

        _pending.Add(context, new PendingAuditBatch(items, ownedTx));
    }

    private async Task PersistPendingAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (!_pending.TryGetValue(context, out var batch))
            return;

        _pending.Remove(context);

        try
        {
            var requests = batch.Items.Select(item =>
            {
                var entityId = item.HasTemporaryKey
                    ? FormatKey(item.KeyProperties, useOriginal: false)
                    : item.CapturedKey;

                // Update temporary PK value inside INSERT details if the key property was included
                var finalChanges = item.Changes;
                if (item.HasTemporaryKey && item.Action == "INSERT")
                {
                    finalChanges = item.Changes.Select(c =>
                    {
                        var keyProp = item.KeyProperties.FirstOrDefault(k => k.Metadata.Name == c.PropertyName);
                        return keyProp is not null ? c with { NewValue = keyProp.CurrentValue } : c;
                    }).ToList();
                }

                return new AuditLogRequest
                {
                    EventId = item.EventId,
                    TenantId = item.Actor.TenantId ?? item.OrganisationId,
                    OrganisationId = item.OrganisationId,
                    DivisionId = item.DivisionId,
                    UserId = item.Actor.UserId,
                    UserName = item.Actor.Username,
                    ActorType = item.Actor.ActorType,
                    Action = item.Action,
                    Outcome = AuditOutcomes.Committed,
                    Module = item.Module,
                    EntityType = item.EntityType,
                    EntityId = string.IsNullOrWhiteSpace(entityId) ? "0" : entityId,
                    Description = $"{item.Action} {item.EntityType} ({entityId})",
                    CorrelationId = item.Actor.CorrelationId,
                    RequestId = item.Actor.RequestId,
                    Source = item.Actor.Source,
                    IpAddress = item.Actor.IpAddress,
                    Url = item.Actor.Url,
                    OccurredAtUtc = item.OccurredAtUtc,
                    IsMandatory = item.IsMandatory,
                    Changes = finalChanges
                };
            }).ToList();

            var result = await auditLogService.RecordWithinTransactionAsync(context, requests, cancellationToken);
            if (!result.Succeeded && batch.Items.Any(i => i.IsMandatory) && _options.FailOnMandatoryAuditError)
            {
                throw new InvalidOperationException($"Mandatory audit persistence failed: {result.Error}");
            }

            if (batch.OwnedTransaction is not null)
            {
                await batch.OwnedTransaction.CommitAsync(cancellationToken);
                await batch.OwnedTransaction.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automatic audit persistence failed for {Count} entity change(s); rolling back owned transaction if active.", batch.Items.Count);
            if (batch.OwnedTransaction is not null)
            {
                try
                {
                    await batch.OwnedTransaction.RollbackAsync(CancellationToken.None);
                    await batch.OwnedTransaction.DisposeAsync();
                }
                catch (Exception rollbackEx)
                {
                    logger.LogWarning(rollbackEx, "Rollback of owned transaction after audit failure encountered an error.");
                }
            }

            if (batch.Items.Any(i => i.IsMandatory) && _options.FailOnMandatoryAuditError)
                throw;
        }
    }

    private async Task CleanupFailedAsync(DbContext context)
    {
        if (!_pending.TryGetValue(context, out var batch))
            return;

        _pending.Remove(context);

        if (batch.OwnedTransaction is not null)
        {
            try
            {
                await batch.OwnedTransaction.RollbackAsync(CancellationToken.None);
                await batch.OwnedTransaction.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error rolling back owned transaction on SaveChangesFailed.");
            }
        }
    }

    private static string FormatKey(List<PropertyEntry> keys, bool useOriginal)
    {
        if (keys.Count == 0) return "0";
        if (keys.Count == 1)
        {
            var val = useOriginal ? keys[0].OriginalValue : keys[0].CurrentValue;
            return val?.ToString() ?? "0";
        }
        return string.Join(":", keys.Select(k => $"{k.Metadata.Name}={(useOriginal ? k.OriginalValue : k.CurrentValue)}"));
    }

    private static int? ResolveIntProperty(EntityEntry entry, string? configuredName, params string[] fallbackNames)
    {
        var candidates = string.IsNullOrWhiteSpace(configuredName)
            ? fallbackNames
            : new[] { configuredName }.Concat(fallbackNames).ToArray();

        foreach (var name in candidates)
        {
            var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (prop is null) continue;
            var raw = entry.State == EntityState.Deleted ? prop.OriginalValue : prop.CurrentValue;
            if (raw is int i && i > 0) return i;
        }
        return null;
    }

    private sealed record PendingAuditBatch(List<PendingAuditItem> Items, IDbContextTransaction? OwnedTransaction);

    private sealed class PendingAuditItem
    {
        public Guid EventId { get; init; }
        public string Action { get; init; } = string.Empty;
        public string Module { get; init; } = string.Empty;
        public string EntityType { get; init; } = string.Empty;
        public List<PropertyEntry> KeyProperties { get; init; } = [];
        public string CapturedKey { get; init; } = "0";
        public bool HasTemporaryKey { get; init; }
        public bool IsMandatory { get; init; }
        public int? OrganisationId { get; init; }
        public int? DivisionId { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public AuditActorContext Actor { get; init; } = AuditActorContext.System();
        public List<AuditPropertyChange> Changes { get; init; } = [];
    }
}
