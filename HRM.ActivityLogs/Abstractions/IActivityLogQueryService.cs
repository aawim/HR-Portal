using HRM.ActivityLogs.Models;

namespace HRM.ActivityLogs.Abstractions;

public interface IActivityLogQueryService
{
    Task<ActivityLogPageResult> GetMyRecentAsync(
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);

    Task<ActivityLogPageResult> SearchAsync(
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);

    Task<ActivityLogPageResult> GetEmployeeTimelineAsync(
        int employeeUserId,
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);

    Task<ActivityLogPageResult> GetOrganisationActivitiesAsync(
        int organisationId,
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);

    Task<ActivityLogPageResult> GetTeamActivitiesAsync(
        int teamId,
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);

    Task<ActivityLogPageResult> GetEntityActivitiesAsync(
        string targetEntityType,
        string targetEntityId,
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);

    Task<ActivityLogDto?> GetByIdAsync(
        long activityLogId,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);

    Task<ActivityExportResult> ExportCsvAsync(
        ActivityLogFilter filter,
        ActivityActorContext actor,
        CancellationToken cancellationToken = default);
}
