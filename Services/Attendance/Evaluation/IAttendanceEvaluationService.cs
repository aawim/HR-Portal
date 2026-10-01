using HRM.DTOs.Attendance;

namespace HRM.Services.Attendance.Evaluation
{
    public interface IAttendanceEvaluationService
    {
        Task<AttendanceDailyEvaluationDto> EvaluateAsync(
          int individualId,
          int jobId,
          DateTime workDate,
          CancellationToken cancellationToken = default);
    }
}
