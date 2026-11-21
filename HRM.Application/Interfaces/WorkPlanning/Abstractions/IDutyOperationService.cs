namespace HRM.WorkPlanning.Abstractions;
public sealed record RequestDutyOperation(long DutyAssignmentId, string OperationType, string Reason, int RequestedByUserId, int OperationLogId, long? ReplacementDutyAssignmentId = null, DateTime? ActualStartAt = null, DateTime? ActualEndAt = null);
public interface IDutyOperationService { Task<long> RequestAsync(RequestDutyOperation request, CancellationToken cancellationToken = default); Task ApplyApprovedAsync(long operationId, CancellationToken cancellationToken = default); }
