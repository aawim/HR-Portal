using System.Net.Http.Json;
using HRM.ActivityLogs.Models;

namespace HRM.UI.Services.Activity;

public sealed class ActivityLogApiClient(HttpClient http) : IActivityLogApiClient
{
    public async Task<ActivityLogPageResult> GetMyRecentAsync(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return await http.GetFromJsonAsync<ActivityLogPageResult>(
            $"api/v1/activity-logs/my?page={page}&pageSize={pageSize}",
            cancellationToken) ?? new ActivityLogPageResult { Page = page, PageSize = pageSize };
    }

    public async Task<ActivityLogPageResult> SearchAsync(ActivityLogFilter filter, CancellationToken cancellationToken = default)
    {
        var url = "api/v1/activity-logs" + BuildQueryString(filter);
        return await http.GetFromJsonAsync<ActivityLogPageResult>(url, cancellationToken)
               ?? new ActivityLogPageResult { Page = filter.Page, PageSize = filter.PageSize };
    }

    public Task<ActivityLogDto?> GetByIdAsync(long activityLogId, CancellationToken cancellationToken = default)
        => http.GetFromJsonAsync<ActivityLogDto>($"api/v1/activity-logs/{activityLogId}", cancellationToken);

    public async Task<ActivityLogPageResult> GetEmployeeTimelineAsync(int userId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/activity-logs/employee/{userId}?page={page}&pageSize={pageSize}";
        return await http.GetFromJsonAsync<ActivityLogPageResult>(url, cancellationToken)
               ?? new ActivityLogPageResult { Page = page, PageSize = pageSize };
    }

    public async Task<ActivityLogPageResult> GetOrganisationActivitiesAsync(int organisationId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/activity-logs/organisation/{organisationId}?page={page}&pageSize={pageSize}";
        return await http.GetFromJsonAsync<ActivityLogPageResult>(url, cancellationToken)
               ?? new ActivityLogPageResult { Page = page, PageSize = pageSize };
    }

    public async Task<ActivityLogPageResult> GetTeamActivitiesAsync(int teamId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/activity-logs/team/{teamId}?page={page}&pageSize={pageSize}";
        return await http.GetFromJsonAsync<ActivityLogPageResult>(url, cancellationToken)
               ?? new ActivityLogPageResult { Page = page, PageSize = pageSize };
    }

    public async Task<ActivityLogPageResult> GetEntityActivitiesAsync(string entityType, string entityId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/activity-logs/entity/{Uri.EscapeDataString(entityType)}/{Uri.EscapeDataString(entityId)}?page={page}&pageSize={pageSize}";
        return await http.GetFromJsonAsync<ActivityLogPageResult>(url, cancellationToken)
               ?? new ActivityLogPageResult { Page = page, PageSize = pageSize };
    }

    public string BuildExportUrl(ActivityLogFilter filter)
        => new Uri(http.BaseAddress ?? new Uri("/", UriKind.Relative), "api/v1/activity-logs/export" + BuildQueryString(filter)).ToString();

    private static string BuildQueryString(ActivityLogFilter filter)
    {
        var parts = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        if (filter.OrganisationId.HasValue) Add("organisationId", filter.OrganisationId.Value.ToString());
        if (filter.DivisionId.HasValue) Add("divisionId", filter.DivisionId.Value.ToString());
        if (filter.TeamId.HasValue) Add("teamId", filter.TeamId.Value.ToString());
        if (filter.UserId.HasValue) Add("userId", filter.UserId.Value.ToString());
        Add("category", filter.Category);
        Add("activityType", filter.ActivityType);
        Add("module", filter.Module);
        Add("action", filter.Action);
        Add("targetEntityType", filter.TargetEntityType);
        Add("targetEntityId", filter.TargetEntityId);
        Add("correlationId", filter.CorrelationId);
        Add("search", filter.SearchText);
        if (filter.FromUtc.HasValue) Add("fromUtc", filter.FromUtc.Value.ToString("O"));
        if (filter.ToUtc.HasValue) Add("toUtc", filter.ToUtc.Value.ToString("O"));
        Add("sortBy", filter.SortBy);
        Add("sortDescending", filter.SortDescending ? "true" : "false");
        Add("page", filter.Page.ToString());
        Add("pageSize", filter.PageSize.ToString());

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
