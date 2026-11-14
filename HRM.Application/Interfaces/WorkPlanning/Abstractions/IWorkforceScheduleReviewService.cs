namespace HRM.WorkPlanning.Abstractions;

public sealed record WorkforceSessionReviewRow(long Id,int WorkPlanSegmentId,long? DutyAssignmentId,string Status,string Strategy,int EvaluationVersion,int ActualMinutes,int CreditedMinutes,string? MatchReason,DateTime EvaluatedAtUtc);
public sealed record WorkforceLedgerReviewRow(long Id,int IndividualId,int JobId,DateTime StartAt,DateTime EndAt,string TimeCategory,int ActualMinutes,int CreditedMinutes,string SourceReference,string Status,string? PayrollExportReference,bool IsPayrollFinalized,long? CorrectsLedgerEntryId);

public interface IWorkforceScheduleReviewService
{
    Task<IReadOnlyList<WorkforceSessionReviewRow>> GetSessionsAsync(int organisationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkforceLedgerReviewRow>> GetLedgerAsync(int organisationId, CancellationToken cancellationToken = default);
}
