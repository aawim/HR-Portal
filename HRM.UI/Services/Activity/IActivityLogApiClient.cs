using HRM.ActivityLogs.Models;

namespace HRM.UI.Services.Activity;

public interface IActivityLogApiClient
{
    Task<ActivityLogPageResult> GetMyRecentAsync(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<ActivityLogPageResult> SearchAsync(ActivityLogFilter filter, CancellationToken cancellationToken = default);
    Task<ActivityLogDto?> GetByIdAsync(long activityLogId, CancellationToken cancellationToken = default);
    Task<ActivityLogPageResult> GetEmployeeTimelineAsync(int userId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<ActivityLogPageResult> GetOrganisationActivitiesAsync(int organisationId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<ActivityLogPageResult> GetTeamActivitiesAsync(int teamId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<ActivityLogPageResult> GetEntityActivitiesAsync(string entityType, string entityId, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    string BuildExportUrl(ActivityLogFilter filter);
}
