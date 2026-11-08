using HRM.DTOs.Attendance;
using HRM.Helpers;
namespace HRM.Application.Contracts.Pages;

public sealed record OrganisationAttendanceFilter(DateOnly Date, int? OrganisationId = null, int? StructureTypeId = null,
    int? StructureId = null, int? TeamId = null, int? JobTypeId = null);
public sealed record OrganisationAttendanceStaff(int StaffId, int JobId, string Name, string? EmployeeNumber,
    AttendanceEligibilityDay Eligibility, List<StaffAttendanceClockEventDto> ClockEvents, List<StaffScheduleAttendanceDto> Schedules)
{
    public YearlyAttendanceDayDto? Overview { get; init; }
}
public sealed record OrganisationAttendancePage(List<DirectoryOption> Structures, List<DirectoryOption> Teams,
    List<DirectoryOption> JobTypes, List<OrganisationAttendanceStaff> Staff);

/// <summary>Returns an authorised roster in one request; schedule and eligibility rules remain in the backend.</summary>
public interface IOrganisationAttendanceQueries
{
    Task<OrganisationAttendancePage> GetDailyAsync(OrganisationAttendanceFilter filter, CancellationToken ct = default);
}
