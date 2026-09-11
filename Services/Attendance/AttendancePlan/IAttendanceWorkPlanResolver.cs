using HRM.DTOs.Attendance;

namespace HRM.Services.Attendance.AttendancePlan
{
    public interface IAttendanceWorkPlanResolver
    {
        Task<AttendancePlanResolutionResult> ResolveAsync(
        int individualId,
        DateTime clockTime,
        CancellationToken cancellationToken = default);
    }
}
