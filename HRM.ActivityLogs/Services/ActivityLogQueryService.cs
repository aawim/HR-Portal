using System.Globalization;
using System.Text;
using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using HRM.ActivityLogs.Persistence;
using HRM.ActivityLogs.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HRM.ActivityLogs.Services;

public sealed class ActivityLogQueryService(
    IDbContextFactory<ActivityDbContext> dbFactory,
    IActivityLogService activityWriter,
    IOptions<ActivityLogOptions> optionsAccessor) : IActivityLogQueryService
{
    private readonly ActivityLogOptions _options = optionsAccessor.Value;

    public async Task<ActivityLogPageResult> GetMyRecentAsync(
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default)
    {
        if (!actor.UserId.HasValue || actor.UserId.Value <= 0)
            throw new UnauthorizedAccessException("Authenticated user context is required.");

        var scoped = CloneFilter(filter);
        scoped.UserId = actor.UserId.Value;
        scoped.OrganisationId = actor.OrganisationId;

        var page = Math.Max(1, scoped.Page);
        var pageSize = Math.Clamp(scoped.PageSize <= 0 ? 25 : scoped.PageSize, 1, _options.MaxPageSize);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.ActivityLogs.AsNoTracking().Where(x => x.UserId == actor.UserId.Value);
        if (actor.OrganisationId.HasValue && actor.OrganisationId.Value > 0)
            query = query.Where(x => x.OrganisationId == actor.OrganisationId.Value);

        query = ApplyCommonFilters(query, scoped);
        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySorting(query, scoped.SortBy, scoped.SortDescending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapDto(x))
            .ToListAsync(cancellationToken);

        return new ActivityLogPageResult { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ActivityLogPageResult> SearchAsync(
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var scope = ActivityAccessPolicy.ResolveSearchScope(actor, filter);

        if (scope.IsCrossOrganisationAccess)
        {
            await activityWriter.RecordAsync(new ActivityLogRequest
            {
                Category = ActivityCategory.Administration,
                ActivityType = ActivityTypes.CrossOrganisationActivityViewed,
                Module = "ActivityLogs",
                Action = "CROSS_ORG_VIEW",
                Description = $"Viewed activity logs for Organisation {(scope.RequestedOrganisationId?.ToString() ?? "ALL")}.",
                OrganisationId = scope.RequestedOrganisationId ?? actor.OrganisationId,
                UserId = actor.UserId
            }, cancellationToken);
        }

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize <= 0 ? 25 : filter.PageSize, 1, _options.MaxPageSize);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildScopedQuery(db, filter, scope);
        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySorting(query, filter.SortBy, filter.SortDescending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapDto(x))
            .ToListAsync(cancellationToken);

        return new ActivityLogPageResult { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<ActivityLogPageResult> GetEmployeeTimelineAsync(
        int employeeUserId, ActivityLogFilter filter, ActivityActorContext actor, CancellationToken cancellationToken = default)
    {
        var scoped = CloneFilter(filter);
        scoped.UserId = employeeUserId;
        return SearchAsync(scoped, actor, cancellationToken);
    }

    public Task<ActivityLogPageResult> GetOrganisationActivitiesAsync(
        int organisationId, ActivityLogFilter filter, ActivityActorContext actor, CancellationToken cancellationToken = default)
    {
        var scoped = CloneFilter(filter);
        scoped.OrganisationId = organisationId;
        return SearchAsync(scoped, actor, cancellationToken);
    }

    public Task<ActivityLogPageResult> GetTeamActivitiesAsync(
        int teamId, ActivityLogFilter filter, ActivityActorContext actor, CancellationToken cancellationToken = default)
    {
        var scoped = CloneFilter(filter);
        scoped.TeamId = teamId;
        return SearchAsync(scoped, actor, cancellationToken);
    }

    public Task<ActivityLogPageResult> GetEntityActivitiesAsync(
        string targetEntityType, string targetEntityId, ActivityLogFilter filter, ActivityActorContext actor, CancellationToken cancellationToken = default)
    {
        var scoped = CloneFilter(filter);
        scoped.TargetEntityType = targetEntityType;
        scoped.TargetEntityId = targetEntityId;
        return SearchAsync(scoped, actor, cancellationToken);
    }

    public async Task<ActivityLogDto?> GetByIdAsync(
        long activityLogId, ActivityActorContext actor, CancellationToken cancellationToken = default)
    {
        if (activityLogId <= 0) return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ActivityLogs.AsNoTracking().SingleOrDefaultAsync(x => x.ActivityLogId == activityLogId, cancellationToken);
        if (row is null) return null;

        ActivityAccessPolicy.EnsureCanReadRecord(row, actor);
        return MapDto(row);
    }

    public async Task<ActivityExportResult> ExportCsvAsync(
        ActivityLogFilter filter, ActivityActorContext actor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ActivityAccessPolicy.EnsureCanExport(actor);
        var scope = ActivityAccessPolicy.ResolveSearchScope(actor, filter);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ApplySorting(BuildScopedQuery(db, filter, scope), filter.SortBy, filter.SortDescending)
            .Take(_options.MaxExportRows)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("ActivityLogId,EventId,OccurredAtUtc,OrganisationId,DivisionId,TeamId,UserId,ActorType,Category,ActivityType,Module,Action,Description,TargetEntityType,TargetEntityId,TargetDisplayName,CorrelationId,Source");

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                Csv(r.ActivityLogId.ToString(CultureInfo.InvariantCulture)),
                Csv(r.EventId.ToString()),
                Csv(r.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                Csv(r.OrganisationId?.ToString(CultureInfo.InvariantCulture)),
                Csv(r.DivisionId?.ToString(CultureInfo.InvariantCulture)),
                Csv(r.TeamId?.ToString(CultureInfo.InvariantCulture)),
                Csv(r.UserId?.ToString(CultureInfo.InvariantCulture)),
                Csv(r.ActorType),
                Csv(r.ActivityCategory),
                Csv(r.ActivityType),
                Csv(r.Module),
                Csv(r.Action),
                Csv(r.Description),
                Csv(r.TargetEntityType),
                Csv(r.TargetEntityId),
                Csv(r.TargetDisplayName),
                Csv(r.CorrelationId),
                Csv(r.Source)));
        }

        await activityWriter.RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Administration,
            ActivityType = ActivityTypes.ActivityExported,
            Module = "ActivityLogs",
            Action = "EXPORT_CSV",
            Description = $"Exported {rows.Count} activity log record(s) to CSV.",
            OrganisationId = scope.RequestedOrganisationId ?? actor.OrganisationId,
            UserId = actor.UserId
        }, cancellationToken);

        return new ActivityExportResult
        {
            FileName = $"activity-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
            Content = Encoding.UTF8.GetBytes(sb.ToString()),
            RowCount = rows.Count
        };
    }

    private static IQueryable<ActivityLog> BuildScopedQuery(ActivityDbContext db, ActivityLogFilter filter, ResolvedActivityScope scope)
    {
        var query = db.ActivityLogs.AsNoTracking().AsQueryable();

        if (scope.SelfOnly && scope.SelfUserId.HasValue)
        {
            query = query.Where(x => x.UserId == scope.SelfUserId.Value);
        }

        if (!scope.IsGlobalSuperAdmin)
        {
            var orgIds = scope.AllowedOrganisationIds.ToList();
            if (orgIds.Count > 0)
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

        if (scope.AllowedTeamIds.Count > 0)
        {
            var teamIds = scope.AllowedTeamIds.ToList();
            query = query.Where(x => x.TeamId.HasValue && teamIds.Contains(x.TeamId.Value));
        }

        if (!scope.SelfOnly && filter.UserId.HasValue && filter.UserId.Value > 0)
            query = query.Where(x => x.UserId == filter.UserId.Value);

        return ApplyCommonFilters(query, filter);
    }

    private static IQueryable<ActivityLog> ApplyCommonFilters(IQueryable<ActivityLog> query, ActivityLogFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var cat = filter.Category.Trim();
            query = query.Where(x => x.ActivityCategory == cat);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActivityType))
        {
            var type = filter.ActivityType.Trim();
            query = query.Where(x => x.ActivityType == type);
        }

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var mod = filter.Module.Trim();
            query = query.Where(x => x.Module == mod);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var act = filter.Action.Trim().ToUpperInvariant();
            query = query.Where(x => x.Action == act);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetEntityType))
        {
            var tType = filter.TargetEntityType.Trim();
            query = query.Where(x => x.TargetEntityType == tType);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetEntityId))
        {
            var tId = filter.TargetEntityId.Trim();
            query = query.Where(x => x.TargetEntityId == tId);
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
                x.ActivityType.Contains(term) ||
                x.Module.Contains(term) ||
                (x.TargetDisplayName != null && x.TargetDisplayName.Contains(term)) ||
                (x.TargetEntityId != null && x.TargetEntityId.Contains(term)) ||
                (x.CorrelationId != null && x.CorrelationId.Contains(term)));
        }

        return query;
    }

    private static IOrderedQueryable<ActivityLog> ApplySorting(IQueryable<ActivityLog> query, string? sortBy, bool desc)
        => (sortBy ?? "OccurredAtUtc").Trim().ToLowerInvariant() switch
        {
            "category" => desc ? query.OrderByDescending(x => x.ActivityCategory).ThenByDescending(x => x.ActivityLogId) : query.OrderBy(x => x.ActivityCategory).ThenBy(x => x.ActivityLogId),
            "module" => desc ? query.OrderByDescending(x => x.Module).ThenByDescending(x => x.ActivityLogId) : query.OrderBy(x => x.Module).ThenBy(x => x.ActivityLogId),
            "action" => desc ? query.OrderByDescending(x => x.Action).ThenByDescending(x => x.ActivityLogId) : query.OrderBy(x => x.Action).ThenBy(x => x.ActivityLogId),
            _ => desc ? query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.ActivityLogId) : query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.ActivityLogId)
        };

    private static ActivityLogDto MapDto(ActivityLog x) => new()
    {
        ActivityLogId = x.ActivityLogId,
        EventId = x.EventId,
        TenantId = x.TenantId,
        OrganisationId = x.OrganisationId,
        DivisionId = x.DivisionId,
        TeamId = x.TeamId,
        UserId = x.UserId,
        ActorType = x.ActorType,
        ActivityCategory = x.ActivityCategory,
        ActivityType = x.ActivityType,
        Module = x.Module,
        Action = x.Action,
        Description = x.Description,
        TargetEntityType = x.TargetEntityType,
        TargetEntityId = x.TargetEntityId,
        TargetDisplayName = x.TargetDisplayName,
        CorrelationId = x.CorrelationId,
        SessionId = x.SessionId,
        Source = x.Source,
        MetadataJson = x.MetadataJson,
        OccurredAtUtc = x.OccurredAtUtc,
        CreatedAtUtc = x.CreatedAtUtc
    };

    private static ActivityLogFilter CloneFilter(ActivityLogFilter f) => new()
    {
        OrganisationId = f.OrganisationId,
        DivisionId = f.DivisionId,
        TeamId = f.TeamId,
        UserId = f.UserId,
        Category = f.Category,
        ActivityType = f.ActivityType,
        Module = f.Module,
        Action = f.Action,
        TargetEntityType = f.TargetEntityType,
        TargetEntityId = f.TargetEntityId,
        CorrelationId = f.CorrelationId,
        SearchText = f.SearchText,
        FromUtc = f.FromUtc,
        ToUtc = f.ToUtc,
        SortBy = f.SortBy,
        SortDescending = f.SortDescending,
        Page = f.Page,
        PageSize = f.PageSize
    };

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
