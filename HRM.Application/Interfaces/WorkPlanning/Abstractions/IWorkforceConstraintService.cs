namespace HRM.WorkPlanning.Abstractions;
public sealed record WorkforceConstraintViolation(string Code,string Severity,string Message,long? DutyOccurrenceId=null,int? IndividualId=null);
public interface IWorkforceConstraintService{Task<IReadOnlyList<WorkforceConstraintViolation>>ValidateAsync(int rosterVersionId,CancellationToken ct=default);}
