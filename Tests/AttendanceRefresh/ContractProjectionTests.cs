using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Transport;

static class ContractProjectionTests
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var balance = ContractDtoMappings.ToDto(new JobLeaveType
        {
            JobLeaveTypeId = 9, JobId = 18, LeaveTypeId = 4, RemainingDays = 17.5m, IsValid = true,
            EffectiveFromDate = new(2026, 1, 1), EffectiveToDate = new(2027, 1, 1),
            LeaveType = new() { LeaveTypeId = 4, Name = "Annual Leave", NameDhivehi = "Annual DV" }
        });
        Check(balance.JobLeaveTypeId == 9 && balance.JobId == 18 && balance.LeaveTypeId == 4 && balance.RemainingDays == 17.5m &&
            balance.LeaveTypeName == "Annual Leave" && balance.LeaveTypeNameDv == "Annual DV" && balance.EffectiveToDate == new DateTime(2027, 1, 1), "Leave balance projection lost identity, labels, fractions or effective period.");
        var legacyLeave = ContractDtoMappings.ToDto(new Leaf { LeaveId = 6, JobId = 18, Duration = 2, FromDate = new(2026, 10, 28), ToDate = new(2026, 10, 29) });
        Check(legacyLeave.LeaveId == 6 && legacyLeave.Duration == 2 && legacyLeave.LeaveType is null, "Historical leave without loaded navigation failed to project.");
        var clock = ContractDtoMappings.ToDto(new AttendanceLog
        {
            AttendanceLogId = 10, Date = new(2026, 10, 27, 8, 31, 1),
            AttendanceLogResolutions = [new() { WorkAssignmentId = 90, AttendanceClockTypeId = 1, IsValid = true }, new() { WorkAssignmentId = 91, AttendanceClockTypeId = 2, IsValid = false }]
        });
        Check(clock.AttendanceLogResolutions.Count == 2 && clock.AttendanceLogResolutions.First().WorkAssignmentId == 90 &&
            !clock.AttendanceLogResolutions.Last().IsValid, "Clock projection merged or lost schedule resolutions.");
        var policy = ContractDtoMappings.ToDomain(new HRM.DTOs.Contracts.JobTypeOvertimeRuleDto
        { Id = 3, OrganisationId = 10, JobTypeId = 2, IsAllowed = true, MaximumWeeklyHours = 8.5m, Version = 7 });
        Check(policy.Id == 3 && policy.OrganisationId == 10 && policy.Version == 7 && policy.MaximumWeeklyHours == 8.5m, "Policy command lost tenant, identity or optimistic concurrency.");

        var calls = 0;
        var application = new LeaveApplicationDto();
        var result = ServiceResult.Failed("Request needs HR assistance.");
        var backend = Proxy.For<ILeaveService>((method, args) => method switch
        {
            "SubmitLeave" => Submit(args),
            "GetMyLeaveRequestsAsync" => Task.FromResult(new List<Leaf> { new() { LeaveId = 6, Duration = 2 } }),
            "GetApplicantLeaveBalancesAsync" => throw new UnauthorizedAccessException("Wrong tenant"),
            _ => throw new Exception(method)
        });
        Task<ServiceResult> Submit(object?[] args)
        {
            calls++;
            Check(ReferenceEquals(args[0], application) && (int?)args[1] == 18, "Leave adapter changed the applicant or request.");
            return Task.FromResult(result);
        }
        var service = new LeaveApplicationsService(backend);
        Check(ReferenceEquals(await service.SubmitLeave(application, 18), result) && calls == 1, "Submission did not use the canonical workflow exactly once.");
        Check((await service.GetMyLeaveRequestsAsync()).Single().LeaveId == 6, "Leave application read adapter failed.");
        var denied = false;
        try { await service.GetApplicantLeaveBalancesAsync(18); } catch (UnauthorizedAccessException) { denied = true; }
        Check(denied, "DTO adapter swallowed backend permission denial.");
        Console.WriteLine("PASS Contract projections: leave identities/balances, historical rows, clock resolutions, concurrency and canonical workflow delegation");
    }
}
