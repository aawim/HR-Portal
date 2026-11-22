using System.Globalization;
using System.Text;
using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Models;
using HRM.AuditLogs.Options;
using HRM.AuditLogs.Persistence;
using HRM.AuditLogs.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HRM.AuditLogs.Services;

public sealed class AuditLogQueryService(
    IDbContextFactory<AuditDbContext> dbFactory,
    IAuditLogService auditWriter,
    IOptions<AuditLogOptions> optionsAccessor) : IAuditLogQueryService
{
    private readonly AuditLogOptions _options = optionsAccessor.Value;

    public async Task<AuditLogPageResult> SearchAsync(
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var scope = AuditAccessPolicy.ResolveQueryScope(actor, filter);

        if (scope.IsCrossOrganisationAccess)
        {
            await auditWriter.RecordAsync(new AuditLogRequest
            {
                Action = "CROSS_ORG_AUDIT_VIEW",
                Outcome = AuditOutcomes.Committed,
                Module = "AuditLogs",
                EntityType = "AuditLog",
                EntityId = scope.RequestedOrganisationId?.ToString(CultureInfo.InvariantCulture) ?? "ALL",
                OrganisationId = scope.RequestedOrganisationId ?? actor.OrganisationId,
                UserId = actor.UserId,
                UserName = actor.Username,
                ActorType = actor.ActorType,
                Description = $"Cross-organisation audit search executed for Organisation {(scope.RequestedOrganisationId?.ToString() ?? "ALL")}.",
                IsGlobalSystemEvent = !scope.RequestedOrganisationId.HasValue && actor.IsSuperAdministrator,
                IsMandatory = false
            }, cancellationToken);
        }

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize <= 0 ? 25 : filter.PageSize, 1, _options.MaxPageSize);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildScopedQuery(db, filter, scope);

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = ApplySorting(query, filter.SortBy, filter.SortDescending);

        IQueryable<AuditLog> paged = ordered.Skip((page - 1) * pageSize).Take(pageSize);
        if (filter.IncludeDetails)
            paged = paged.Include(x => x.Details);

        var rows = await paged.ToListAsync(cancellationToken);

        Dictionary<long, int> detailCounts = new();
        if (!filter.IncludeDetails && rows.Count > 0)
        {
            var ids = rows.Select(r => r.AuditLogId).ToList();
            detailCounts = await db.AuditLogDetails.AsNoTracking()
                .Where(d => ids.Contains(d.AuditLogId))
                .GroupBy(d => d.AuditLogId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }

        var items = rows.Select(r => MapDto(r, actor, filter.IncludeDetails, detailCounts.GetValueOrDefault(r.AuditLogId))).ToList();
        return new AuditLogPageResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AuditLogRecordDto?> GetByIdAsync(
        long auditLogId,
        AuditActorContext actor,
        CancellationToken cancellationToken = default)
    {
        if (auditLogId <= 0) return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.AuditLogs.AsNoTracking()
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.AuditLogId == auditLogId, cancellationToken);

        if (row is null) return null;

        AuditAccessPolicy.EnsureCanReadRecord(row, actor);
        return MapDto(row, actor, includeDetails: true, row.Details.Count);
    }

    public Task<AuditLogPageResult> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var scoped = CloneFilter(filter);
        scoped.EntityType = entityType;
        scoped.EntityId = entityId;
        scoped.IncludeDetails = true;
        return SearchAsync(scoped, actor, cancellationToken);
    }

    public Task<AuditLogPageResult> GetUserHistoryAsync(
        int targetUserId,
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var scoped = CloneFilter(filter);
        scoped.UserId = targetUserId;
        return SearchAsync(scoped, actor, cancellationToken);
    }

    public Task<AuditLogPageResult> GetOrganisationHistoryAsync(
        int organisationId,
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var scoped = CloneFilter(filter);
        scoped.OrganisationId = organisationId;
        return SearchAsync(scoped, actor, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogRecordDto>> GetByCorrelationIdAsync(
        string correlationId,
        AuditActorContext actor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId)) return Array.Empty<AuditLogRecordDto>();

        var result = await SearchAsync(new AuditLogFilter
        {
            CorrelationId = correlationId.Trim(),
            IncludeDetails = true,
            Page = 1,
            PageSize = Math.Min(100, _options.MaxPageSize),
            SortBy = "OccurredAtUtc",
            SortDescending = false
        }, actor, cancellationToken);

        return result.Items;
    }

    public async Task<AuditExportResult> ExportCsvAsync(
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        AuditAccessPolicy.EnsureCanExport(actor);
        var scope = AuditAccessPolicy.ResolveQueryScope(actor, filter);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildScopedQuery(db, filter, scope);
        var rows = await ApplySorting(query, filter.SortBy, filter.SortDescending)
            .Include(x => x.Details)
            .Take(_options.MaxExportRows)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("AuditLogId,EventId,OccurredAtUtc,OrganisationId,DivisionId,UserId,UserName,ActorType,IpAddress,Url,Action,Outcome,Module,EntityType,EntityId,Description,Reason,CorrelationId,Source,ChangedProperties");

        foreach (var row in rows)
        {
            var dto = MapDto(row, actor, includeDetails: true, row.Details.Count);
            var changedSummary = string.Join("; ", dto.Details.Select(d => $"{d.PropertyName}: {d.OldValue ?? "(null)"} -> {d.NewValue ?? "(null)"}"));
            sb.AppendLine(string.Join(",",
                Csv(dto.AuditLogId.ToString(CultureInfo.InvariantCulture)),
                Csv(dto.EventId.ToString()),
                Csv(dto.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                Csv(dto.OrganisationId?.ToString(CultureInfo.InvariantCulture)),
                Csv(dto.DivisionId?.ToString(CultureInfo.InvariantCulture)),
                Csv(dto.UserId?.ToString(CultureInfo.InvariantCulture)),
                Csv(dto.UserName),
                Csv(dto.ActorType),
                Csv(dto.IpAddress),
                Csv(dto.Url),
                Csv(dto.Action),
                Csv(dto.Outcome),
                Csv(dto.Module),
                Csv(dto.EntityType),
                Csv(dto.EntityId),
                Csv(dto.Description),
                Csv(dto.Reason),
                Csv(dto.CorrelationId),
                Csv(dto.Source),
                Csv(changedSummary)));
        }

        await auditWriter.RecordAsync(new AuditLogRequest
        {
            Action = "AUDIT_EXPORT",
            Outcome = AuditOutcomes.Committed,
            Module = "AuditLogs",
            EntityType = "AuditLog",
            EntityId = scope.RequestedOrganisationId?.ToString(CultureInfo.InvariantCulture) ?? actor.OrganisationId?.ToString(CultureInfo.InvariantCulture) ?? "GLOBAL",
            OrganisationId = scope.RequestedOrganisationId ?? actor.OrganisationId,
            UserId = actor.UserId,
            UserName = actor.Username,
            ActorType = actor.ActorType,
            Description = $"Exported {rows.Count} audit log record(s) to CSV.",
            IsGlobalSystemEvent = !scope.RequestedOrganisationId.HasValue && !actor.OrganisationId.HasValue && actor.IsSuperAdministrator,
            IsMandatory = false
        }, cancellationToken);

        return new AuditExportResult
        {
            FileName = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
            Content = Encoding.UTF8.GetBytes(sb.ToString()),
            RowCount = rows.Count
        };
    }

    private static IQueryable<AuditLog> BuildScopedQuery(AuditDbContext db, AuditLogFilter filter, ResolvedAuditQueryScope scope)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!scope.IsGlobalSuperAdmin)
        {
            var orgIds = scope.AllowedOrganisationIds.ToList();
            query = query.Where(x => x.OrganisationId.HasValue && orgIds.Contains(x.OrganisationId.Value));
        }
        else if (scope.AllowedOrganisationIds.Count > 0)
        {
            var orgIds = scope.AllowedOrganisationIds.ToList();
            query = query.Where(x => x.OrganisationId.HasValue && orgIds.Contains(x.OrganisationId.Value));
        }

        if (scope.AllowedDivisionIds.Count > 0)
        {
            var divIds = scope.AllowedDivisionIds.ToList();
            query = query.Where(x => x.DivisionId.HasValue && divIds.Contains(x.DivisionId.Value));
        }

        if (filter.UserId.HasValue && filter.UserId.Value > 0)
            query = query.Where(x => x.UserId == filter.UserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var act = filter.Action.Trim().ToUpperInvariant();
            query = query.Where(x => x.Action == act);
        }

        if (!string.IsNullOrWhiteSpace(filter.Outcome))
        {
            var outcome = filter.Outcome.Trim();
            query = query.Where(x => x.Outcome == outcome);
        }

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var mod = filter.Module.Trim();
            query = query.Where(x => x.Module == mod);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            var entityType = filter.EntityType.Trim();
            query = query.Where(x => x.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            var entityId = filter.EntityId.Trim();
            query = query.Where(x => x.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            var corr = filter.CorrelationId.Trim();
            query = query.Where(x => x.CorrelationId == corr);
        }

        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc <= filter.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x =>
                x.Description.Contains(term) ||
                x.EntityType.Contains(term) ||
                x.EntityId.Contains(term) ||
                x.Module.Contains(term) ||
                x.Action.Contains(term) ||
                (x.UserName != null && x.UserName.Contains(term)) ||
                (x.IpAddress != null && x.IpAddress.Contains(term)) ||
                (x.Url != null && x.Url.Contains(term)) ||
                (x.Reason != null && x.Reason.Contains(term)) ||
                (x.CorrelationId != null && x.CorrelationId.Contains(term)));
        }

        return query;
    }

    private static IOrderedQueryable<AuditLog> ApplySorting(IQueryable<AuditLog> query, string? sortBy, bool desc)
    {
        return (sortBy ?? "OccurredAtUtc").Trim().ToLowerInvariant() switch
        {
            "action" => desc ? query.OrderByDescending(x => x.Action).ThenByDescending(x => x.AuditLogId) : query.OrderBy(x => x.Action).ThenBy(x => x.AuditLogId),
            "module" => desc ? query.OrderByDescending(x => x.Module).ThenByDescending(x => x.AuditLogId) : query.OrderBy(x => x.Module).ThenBy(x => x.AuditLogId),
            "entitytype" => desc ? query.OrderByDescending(x => x.EntityType).ThenByDescending(x => x.AuditLogId) : query.OrderBy(x => x.EntityType).ThenBy(x => x.AuditLogId),
            "userid" => desc ? query.OrderByDescending(x => x.UserId).ThenByDescending(x => x.AuditLogId) : query.OrderBy(x => x.UserId).ThenBy(x => x.AuditLogId),
            _ => desc ? query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.AuditLogId) : query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.AuditLogId)
        };
    }

    private AuditLogRecordDto MapDto(AuditLog row, AuditActorContext actor, bool includeDetails, int detailCount)
    {
        var canSeeConfidential = actor.IsSuperAdministrator || actor.CanViewConfidentialValues;

        var details = includeDetails
            ? row.Details.OrderBy(d => d.PropertyName).Select(d =>
            {
                var maskConfidential = !d.IsRedacted && !canSeeConfidential &&
                                       AuditDataRedaction.IsConfidentialProperty(d.PropertyName, _options.Policy);
                return new AuditLogDetailDto
                {
                    AuditLogDetailId = d.AuditLogDetailId,
                    PropertyName = d.PropertyName,
                    OldValue = maskConfidential ? (d.OldValue is null ? null : AuditDataRedaction.ConfidentialPlaceholder) : d.OldValue,
                    NewValue = maskConfidential ? (d.NewValue is null ? null : AuditDataRedaction.ConfidentialPlaceholder) : d.NewValue,
                    ValueType = d.ValueType,
                    IsRedacted = d.IsRedacted || maskConfidential
                };
            }).ToList()
            : [];

        return new AuditLogRecordDto
        {
            AuditLogId = row.AuditLogId,
            EventId = row.EventId,
            TenantId = row.TenantId,
            OrganisationId = row.OrganisationId,
            DivisionId = row.DivisionId,
            UserId = row.UserId,
            UserName = row.UserName,
            ActorType = row.ActorType,
            Action = row.Action,
            Outcome = row.Outcome,
            Module = row.Module,
            EntityType = row.EntityType,
            EntityId = row.EntityId,
            Description = row.Description,
            CorrelationId = row.CorrelationId,
            RequestId = row.RequestId,
            Source = row.Source,
            Reason = row.Reason,
            IpAddress = row.IpAddress,
            Url = row.Url,
            OccurredAtUtc = row.OccurredAtUtc,
            MetadataJson = canSeeConfidential ? row.MetadataJson : null,
            SchemaVersion = row.SchemaVersion,
            RetentionUntilUtc = row.RetentionUntilUtc,
            IsLegalHold = row.IsLegalHold,
            ArchivedAtUtc = row.ArchivedAtUtc,
            DetailCount = includeDetails ? details.Count : detailCount,
            Details = details
        };
    }

    private static AuditLogFilter CloneFilter(AuditLogFilter source) => new()
    {
        OrganisationId = source.OrganisationId,
        DivisionId = source.DivisionId,
        UserId = source.UserId,
        Action = source.Action,
        Outcome = source.Outcome,
        Module = source.Module,
        EntityType = source.EntityType,
        EntityId = source.EntityId,
        CorrelationId = source.CorrelationId,
        SearchText = source.SearchText,
        FromUtc = source.FromUtc,
        ToUtc = source.ToUtc,
        IncludeDetails = source.IncludeDetails,
        SortBy = source.SortBy,
        SortDescending = source.SortDescending,
        Page = source.Page,
        PageSize = source.PageSize
    };

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        // Neutralize spreadsheet formula injection
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
