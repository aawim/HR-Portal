using HRM.DTOs.Attendance;

namespace HRM.Services.Attendance.Abstraction
{
    public interface IAttendanceSegmentEvaluator
    {
        Task<List<AttendanceSegmentEvaluationDto>> EvaluateAsync(
            int workPlanId,
            CancellationToken cancellationToken = default);
    }
}
