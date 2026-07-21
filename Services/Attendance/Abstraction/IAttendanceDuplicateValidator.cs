using HRM.Services.Attendance.Validation;

namespace HRM.Services.Attendance.Abstraction
{
    public interface IAttendanceDuplicateValidator
    {
        Task<AttendanceDuplicateValidationResult> ValidateAsync(
        int attendanceLogId,
        CancellationToken cancellationToken = default);
    }
}
