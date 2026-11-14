namespace HRM.WorkPlanning.Abstractions;

public sealed record CreateWorkforceRosterRequest(int OrganisationId, string Name, DateOnly StartsOn, DateOnly EndsOn, int ActorUserId, int OperationLogId);
public sealed record WorkforceRosterDraft(int RosterId, int RosterVersionId, int VersionNumber);
public sealed record WorkforceRosterValidation(bool IsValid, IReadOnlyList<string> Errors, IReadOnlyList<string> CoverageIssues, IReadOnlyList<string> OverlapIssues, IReadOnlyList<string> LeaveIssues, IReadOnlyList<string>? Warnings = null);
public sealed record AddDutyOccurrenceRequest(int RosterVersionId, int OrganisationId, int DutyTypeId, DateTime StartAt, DateTime EndAt, int RequiredStaffCount, string TimeZoneId, string? LocationName, string? SourceKey, int OperationLogId);
public sealed record AssignDutyRequest(long DutyOccurrenceId, int OrganisationId, int JobId, int IndividualId, int ActorUserId, string? Reason, int OperationLogId);

public interface IWorkforceRosterService
{
    Task<WorkforceRosterDraft> CreateDraftAsync(CreateWorkforceRosterRequest request, CancellationToken cancellationToken = default);
    Task<WorkforceRosterValidation> ValidateAsync(int rosterVersionId, int? organisationId = null, CancellationToken cancellationToken = default);
    Task<long> AddOccurrenceAsync(AddDutyOccurrenceRequest request, CancellationToken cancellationToken = default);
    Task<long> AssignAsync(AssignDutyRequest request, CancellationToken cancellationToken = default);
    Task PublishAsync(int rosterVersionId, int actorUserId, int operationLogId, CancellationToken cancellationToken = default);
}
