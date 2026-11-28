namespace HRM.WorkPlanning.Abstractions;
public sealed record RecordWorkTimeLedgerEntry(int OrganisationId,int IndividualId,int JobId,DateTime StartAt,DateTime EndAt,string TimeCategory,int ActualMinutes,int CreditedMinutes,string SourceReference,int OperationLogId,int? WorkPlanSegmentId=null,long? DutyAssignmentId=null,int? WorkforcePolicyId=null,int? WorkforcePolicyVersion=null);
public interface IWorkTimeLedgerService { Task<long> RecordAsync(RecordWorkTimeLedgerEntry entry,CancellationToken cancellationToken=default); }
