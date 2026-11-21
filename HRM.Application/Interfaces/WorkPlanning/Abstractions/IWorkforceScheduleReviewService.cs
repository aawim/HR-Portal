namespace HRM.WorkPlanning.Abstractions;

public sealed record WorkforceSessionReviewRow(long Id,int WorkPlanSegmentId,long? DutyAssignmentId,string Status,string Strategy,int EvaluationVersion,int ActualMinutes,int CreditedMinutes,string? MatchReason,DateTime EvaluatedAtUtc);
public sealed record WorkforceLedgerReviewRow(long Id,int IndividualId,int JobId,DateTime StartAt,DateTime EndAt,string TimeCategory,int ActualMinutes,int CreditedMinutes,string SourceReference,string Status,string? PayrollExportReference,bool IsPayrollFinalized,long? CorrectsLedgerEntryId);
public sealed record WorkforceReviewQuery(DateTime? From = null, DateTime? To = null, int Page = 1, int PageSize = 50);
public sealed record WorkforceReviewPage<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public interface IWorkforceScheduleReviewService
{
    Task<IReadOnlyList<WorkforceSessionReviewRow>> GetSessionsAsync(int organisationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkforceLedgerReviewRow>> GetLedgerAsync(int organisationId, CancellationToken cancellationToken = default);
    Task<WorkforceReviewPage<WorkforceSessionReviewRow>> GetSessionsPageAsync(int organisationId, WorkforceReviewQuery query, CancellationToken cancellationToken = default);
    Task<WorkforceReviewPage<WorkforceLedgerReviewRow>> GetLedgerPageAsync(int organisationId, WorkforceReviewQuery query, CancellationToken cancellationToken = default);
}
