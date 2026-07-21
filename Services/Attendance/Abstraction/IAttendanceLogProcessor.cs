using HRM.Services.Attendance.Processing;

namespace HRM.Services.Attendance.Abstraction
{
    public interface IAttendanceLogProcessor
    {
        Task<AttendanceProcessingResult> ProcessAsync(
        int attendanceLogId,
        CancellationToken cancellationToken = default);
    }
}
