using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Models;
using HRM.AuditLogs.Options;
using HRM.AuditLogs.Persistence;
using HRM.AuditLogs.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AuditLogs.Services;

public sealed class AuditLogService(
    IDbContextFactory<AuditDbContext> dbFactory,
    IAuditContextAccessor contextAccessor,
    IAuditClock clock,
    IOptions<AuditLogOptions> optionsAccessor,
    ILogger<AuditLogService> logger) : IAuditLogService
{
    private readonly AuditLogOptions _options = optionsAccessor.Value;

    public async Task<AuditLogResult> RecordAsync(
        AuditLogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var eventId = request.EventId ?? Guid.NewGuid();
        var actor = await contextAccessor.GetCurrentContextAsync(cancellationToken);
        var entity = BuildAuditLogEntity(request, eventId, actor);

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var existingId = await db.AuditLogs.AsNoTracking()
                .Where(x => x.EventId == entity.EventId)
                .Select(x => (long?)x.AuditLogId)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingId.HasValue)
                return AuditLogResult.Success(existingId.Value, entity.EventId, duplicate: true);

            db.AuditLogs.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return AuditLogResult.Success(entity.AuditLogId, entity.EventId);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await using var checkDb = await dbFactory.CreateDbContextAsync(cancellationToken);
            var existingId = await checkDb.AuditLogs.AsNoTracking()
                .Where(x => x.EventId == entity.EventId)
                .Select(x => (long?)x.AuditLogId)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingId.HasValue)
                return AuditLogResult.Success(existingId.Value, entity.EventId, duplicate: true);

            logger.LogError(ex, "Audit log persistence conflict for EventId {EventId}.", entity.EventId);
            if (request.IsMandatory && _options.FailOnMandatoryAuditError) throw;
            return AuditLogResult.Failure(entity.EventId, "Audit log persistence conflict.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist audit log EventId {EventId} ({Module}/{EntityType}/{Action}).",
                entity.EventId, entity.Module, entity.EntityType, entity.Action);
            if (request.IsMandatory && _options.FailOnMandatoryAuditError) throw;
            return AuditLogResult.Failure(entity.EventId, "Audit log persistence failed.");
        }
    }

    public async Task<AuditLogResult> RecordBatchAsync(
        IEnumerable<AuditLogRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var list = requests.ToList();
        if (list.Count == 0) return AuditLogResult.Success(0, Guid.Empty);

        var actor = await contextAccessor.GetCurrentContextAsync(cancellationToken);
        var entities = list.Select(r => BuildAuditLogEntity(r, r.EventId ?? Guid.NewGuid(), actor)).ToList();
        var isMandatory = list.Any(r => r.IsMandatory);

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var eventIds = entities.Select(e => e.EventId).ToList();
            var existing = (await db.AuditLogs.AsNoTracking()
                .Where(x => eventIds.Contains(x.EventId))
                .Select(x => x.EventId)
                .ToListAsync(cancellationToken)).ToHashSet();

            var toInsert = entities.Where(e => !existing.Contains(e.EventId)).ToList();
            if (toInsert.Count > 0)
            {
                db.AuditLogs.AddRange(toInsert);
                await db.SaveChangesAsync(cancellationToken);
            }

            var first = entities[0];
            return AuditLogResult.Success(first.AuditLogId, first.EventId, duplicate: toInsert.Count == 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist audit log batch ({Count} records).", entities.Count);
            if (isMandatory && _options.FailOnMandatoryAuditError) throw;
            return AuditLogResult.Failure(entities[0].EventId, "Audit log batch persistence failed.");
        }
    }

    public async Task<AuditLogResult> RecordWithinTransactionAsync(
        DbContext businessContext,
        IEnumerable<AuditLogRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(businessContext);
        var list = requests.ToList();
        if (list.Count == 0) return AuditLogResult.Success(0, Guid.Empty);

        var actor = await contextAccessor.GetCurrentContextAsync(cancellationToken);
        var entities = list.Select(r => BuildAuditLogEntity(r, r.EventId ?? Guid.NewGuid(), actor)).ToList();

        if (businessContext.Database.IsRelational())
        {
            var connection = businessContext.Database.GetDbConnection();
            var currentTx = businessContext.Database.CurrentTransaction;
            var optsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
            if (businessContext.Database.IsSqlServer())
            {
                optsBuilder.UseSqlServer(connection);
            }
            else
            {
                // Fallback for non-SQLServer relational providers (e.g. SQLite in tests): use factory
                return await RecordBatchAsync(list, cancellationToken);
            }

            await using var sharedAuditDb = new AuditDbContext(optsBuilder.Options);
            if (currentTx is not null)
            {
                await sharedAuditDb.Database.UseTransactionAsync(currentTx.GetDbTransaction(), cancellationToken);
            }

            var eventIds = entities.Select(e => e.EventId).ToList();
            var existing = (await sharedAuditDb.AuditLogs.AsNoTracking()
                .Where(x => eventIds.Contains(x.EventId))
                .Select(x => x.EventId)
                .ToListAsync(cancellationToken)).ToHashSet();

            var toInsert = entities.Where(e => !existing.Contains(e.EventId)).ToList();
            if (toInsert.Count > 0)
            {
                sharedAuditDb.AuditLogs.AddRange(toInsert);
                await sharedAuditDb.SaveChangesAsync(cancellationToken);
            }

            return AuditLogResult.Success(entities[0].AuditLogId, entities[0].EventId, duplicate: toInsert.Count == 0);
        }

        return await RecordBatchAsync(list, cancellationToken);
    }

    public Task<AuditLogResult> RecordApprovalAsync(
        string module, string entityType, string entityId, string description,
        string? reason = null, int? organisationId = null, int? divisionId = null, object? metadata = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = "APPROVE",
            Outcome = AuditOutcomes.Committed,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId,
            DivisionId = divisionId,
            Metadata = metadata
        }, cancellationToken);

    public Task<AuditLogResult> RecordRejectionAsync(
        string module, string entityType, string entityId, string description,
        string? reason = null, int? organisationId = null, int? divisionId = null, object? metadata = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = "REJECT",
            Outcome = AuditOutcomes.Committed,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId,
            DivisionId = divisionId,
            Metadata = metadata
        }, cancellationToken);

    public Task<AuditLogResult> RecordAttendanceCorrectionAsync(
        string entityType, string entityId, string description,
        IReadOnlyList<AuditPropertyChange> changes, string? reason = null, int? organisationId = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = "OVERRIDE",
            Outcome = AuditOutcomes.Committed,
            Module = "Attendance",
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId,
            Changes = changes.ToList()
        }, cancellationToken);

    public Task<AuditLogResult> RecordSalaryChangeAsync(
        string entityType, string entityId, string description,
        IReadOnlyList<AuditPropertyChange> changes, string? reason = null, int? organisationId = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = "UPDATE",
            Outcome = AuditOutcomes.Committed,
            Module = "Compensation",
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId,
            Changes = changes.ToList()
        }, cancellationToken);

    public Task<AuditLogResult> RecordPayrollAdjustmentAsync(
        string entityType, string entityId, string description,
        IReadOnlyList<AuditPropertyChange> changes, string? reason = null, int? organisationId = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = "ADJUST",
            Outcome = AuditOutcomes.Committed,
            Module = "Payroll",
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId,
            Changes = changes.ToList()
        }, cancellationToken);

    public Task<AuditLogResult> RecordPermissionChangeAsync(
        string action, string entityType, string entityId, string description,
        IReadOnlyList<AuditPropertyChange>? changes = null, string? reason = null, int? organisationId = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = action,
            Outcome = AuditOutcomes.Committed,
            Module = "AccessManagement",
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId,
            Changes = changes?.ToList() ?? []
        }, cancellationToken);

    public Task<AuditLogResult> RecordOrganisationAccessChangeAsync(
        string action, string entityId, string description, int? organisationId = null, string? reason = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = action,
            Outcome = AuditOutcomes.Committed,
            Module = "AccessManagement",
            EntityType = "UserOrganisationAccess",
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId
        }, cancellationToken);

    public Task<AuditLogResult> RecordStaffTransferDecisionAsync(
        string action, string transferId, string description, string? reason = null, int? organisationId = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = action,
            Outcome = AuditOutcomes.Committed,
            Module = "Transfers",
            EntityType = "StaffTransfer",
            EntityId = transferId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId
        }, cancellationToken);

    public Task<AuditLogResult> RecordWorkflowOverrideAsync(
        string module, string entityType, string entityId, string description, string reason, int? organisationId = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = "OVERRIDE",
            Outcome = AuditOutcomes.Committed,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId
        }, cancellationToken);

    public Task<AuditLogResult> RecordSecurityEventAsync(
        string action, string outcome, string module, string entityType, string entityId, string description,
        string? reason = null, int? organisationId = null, object? metadata = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new AuditLogRequest
        {
            Action = action,
            Outcome = string.IsNullOrWhiteSpace(outcome) ? AuditOutcomes.Denied : outcome,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Reason = reason,
            OrganisationId = organisationId,
            Metadata = metadata,
            IsMandatory = false
        }, cancellationToken);

    internal AuditLog BuildAuditLogEntity(AuditLogRequest request, Guid eventId, AuditActorContext actor)
    {
        var nowUtc = request.OccurredAtUtc?.ToUniversalTime() ?? clock.UtcNow;
        var orgId = request.OrganisationId ?? actor.OrganisationId;
        var tenantId = request.TenantId ?? actor.TenantId ?? orgId;
        var divisionId = request.DivisionId ?? actor.DivisionId;
        var userId = request.UserId ?? actor.UserId;
        var actorType = string.IsNullOrWhiteSpace(request.ActorType) ? actor.ActorType : request.ActorType.Trim();

        if (!request.IsGlobalSystemEvent && !tenantId.HasValue && !orgId.HasValue && actorType == AuditActorTypes.User)
        {
            tenantId = actor.TenantId ?? actor.OrganisationId;
            orgId = actor.OrganisationId;
        }

        _options.Policy.AuditedEntities.TryGetValue(request.EntityType, out var entityRule);

        var entry = new AuditLog
        {
            EventId = eventId,
            TenantId = tenantId,
            OrganisationId = orgId,
            DivisionId = divisionId,
            UserId = userId,
            ActorType = string.IsNullOrWhiteSpace(actorType) ? AuditActorTypes.User : actorType,
            Action = string.IsNullOrWhiteSpace(request.Action) ? "EVENT" : request.Action.Trim().ToUpperInvariant(),
            Outcome = string.IsNullOrWhiteSpace(request.Outcome) ? AuditOutcomes.Committed : request.Outcome.Trim(),
            Module = string.IsNullOrWhiteSpace(request.Module) ? (entityRule?.Module ?? "General") : request.Module.Trim(),
            EntityType = string.IsNullOrWhiteSpace(request.EntityType) ? "Unknown" : request.EntityType.Trim(),
            EntityId = string.IsNullOrWhiteSpace(request.EntityId) ? "0" : AuditDataRedaction.Truncate(request.EntityId.Trim(), 128)!,
            Description = AuditDataRedaction.Truncate(string.IsNullOrWhiteSpace(request.Description) ? $"{request.Action} {request.EntityType} {request.EntityId}" : request.Description.Trim(), 1000)!,
            CorrelationId = AuditDataRedaction.Truncate(request.CorrelationId ?? actor.CorrelationId, 128),
            RequestId = AuditDataRedaction.Truncate(request.RequestId ?? actor.RequestId, 128),
            Source = AuditDataRedaction.Truncate(request.Source ?? actor.Source ?? _options.DefaultSource, 64)!,
            Reason = AuditDataRedaction.Truncate(request.Reason?.Trim(), 1000),
            IpAddress = _options.CaptureIpAddress ? AuditDataRedaction.Truncate(request.IpAddress ?? actor.IpAddress, 64) : null,
            OccurredAtUtc = nowUtc,
            MetadataJson = AuditValueSerializer.SerializeMetadata(request.Metadata, request.MetadataJson, _options.MaxMetadataLength),
            SchemaVersion = 1,
            RetentionUntilUtc = _options.DefaultRetentionDays > 0 ? nowUtc.AddDays(_options.DefaultRetentionDays) : null,
            IsLegalHold = request.IsLegalHold
        };

        foreach (var change in request.Changes)
        {
            if (string.IsNullOrWhiteSpace(change.PropertyName)) continue;

            var redact = change.ForceRedact || AuditDataRedaction.ShouldRedactAtWrite(change.PropertyName, _options.Policy, entityRule);
            if (redact)
            {
                entry.Details.Add(new AuditLogDetail
                {
                    PropertyName = AuditDataRedaction.Truncate(change.PropertyName.Trim(), 128)!,
                    OldValue = change.OldValue is null ? null : AuditDataRedaction.RedactedPlaceholder,
                    NewValue = change.NewValue is null ? null : AuditDataRedaction.RedactedPlaceholder,
                    ValueType = change.ValueType ?? "Redacted",
                    IsRedacted = true
                });
            }
            else
            {
                var (oldSer, oldType) = AuditValueSerializer.Serialize(change.OldValue, _options.MaxValueLength);
                var (newSer, newType) = AuditValueSerializer.Serialize(change.NewValue, _options.MaxValueLength);
                var resolvedType = change.ValueType ?? (newType != "Null" ? newType : oldType);

                entry.Details.Add(new AuditLogDetail
                {
                    PropertyName = AuditDataRedaction.Truncate(change.PropertyName.Trim(), 128)!,
                    OldValue = oldSer,
                    NewValue = newSer,
                    ValueType = AuditDataRedaction.Truncate(resolvedType, 64)!,
                    IsRedacted = false
                });
            }
        }

        return entry;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}
