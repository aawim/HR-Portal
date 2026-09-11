using HRM.DTOs.Attendance;

namespace HRM.Services.Attendance.AttendancePlan
{
    public interface IAttendancePlanService
    {
        Task<AttendanceWorkPlanDto?> GetPlanAsync(
        int individualId,
        int jobId,
        DateTime workDate,
        CancellationToken cancellationToken = default);

        Task<AttendanceWorkPlanDto?> GetOrGenerateAsync(
        int individualId,
        int jobId,
        int organisationBusinessEntityId,
        DateTime attendanceTime,
        CancellationToken cancellationToken = default);

    }
}
