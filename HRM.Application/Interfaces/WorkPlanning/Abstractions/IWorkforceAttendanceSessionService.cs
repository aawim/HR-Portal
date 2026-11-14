namespace HRM.WorkPlanning.Abstractions;
public sealed record EvaluateWorkforceSession(int WorkPlanSegmentId,long? DutyAssignmentId,string Status,string Strategy,int ActualMinutes,int CreditedMinutes,string? MatchReason,int OperationLogId);
public interface IWorkforceAttendanceSessionService{Task<long> EvaluateAsync(EvaluateWorkforceSession evaluation,CancellationToken cancellationToken=default);}
