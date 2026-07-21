using HRM.Models;

namespace HRM.Services.Attendance.Repositories
{
    public interface IAttendanceLogResolutionRepository
    {

        Task<AttendanceLogResolution?> GetByAttendanceLogIdAsync(
        int attendanceLogId,
        CancellationToken cancellationToken = default);

        Task<bool> HasActiveResolutionAsync(
            int attendanceLogId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AttendanceLogResolution>>
            GetByAssignmentAsync(
                long workAssignmentId,
                CancellationToken cancellationToken = default);

        Task<AttendanceLogResolution> AddAsync(
            AttendanceLogResolution resolution,
            CancellationToken cancellationToken = default);

        Task InvalidateAsync(
            long attendanceLogResolutionId,
            CancellationToken cancellationToken = default);
    }
}
