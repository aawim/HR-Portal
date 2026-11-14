namespace HRM.WorkPlanning.Abstractions;
public sealed record DutyAvailabilityAssessment(bool IsAvailable,string? Reason);
public interface IDutyAvailabilityService { Task<DutyAvailabilityAssessment> AssessAsync(int organisationId,int individualId,int jobId,DateTime startAt,DateTime endAt,CancellationToken cancellationToken=default); }
