using HRM.DTOs.Attendance;

namespace HRM.Services.Attendance.Abstraction
{
    public interface IAttendanceResolver
    {
        Task<List<AttendanceSegmentResultDto>> ResolveAsync(
       AttendanceWorkPlanDto plan,
       List<AttendanceRawLogDto> logs,
       CancellationToken cancellationToken = default);
    }
}
