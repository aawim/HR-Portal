using HRM.ActivityLogs.Models;

namespace HRM.ActivityLogs.Abstractions;

public interface IActivityLogService
{
    Task RecordAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default);

    Task EnqueueAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default);

    Task RecordInOutboxAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default);

    Task RecordAuthenticationAsync(
        string activityType,
        string description,
        int? userId = null,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task RecordOrganisationSwitchAsync(
        int targetOrganisationId,
        string organisationName,
        CancellationToken cancellationToken = default);

    Task RecordTeamSwitchAsync(
        int teamId,
        string teamName,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task RecordPayrollInitiatedAsync(
        string payrollRunId,
        string periodLabel,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task RecordTransferSubmittedAsync(
        string transferId,
        string targetDisplayName,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task RecordWorkflowDecisionAsync(
        ActivityCategory category,
        string activityType,
        string module,
        string action,
        string targetEntityType,
        string targetEntityId,
        string? targetDisplayName,
        string description,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task RecordDocumentDownloadAsync(
        string documentId,
        string documentTitle,
        int? organisationId = null,
        CancellationToken cancellationToken = default);
}
