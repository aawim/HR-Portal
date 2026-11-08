using HRM.Services.Attendance.Abstraction;
using HRM.Services.Attendance.Repositories;
using HRM.Services.Attendance.Validation;

namespace HRM.Services.Attendance.Abstraction.Services
{
    public class AttendanceDuplicateValidator : IAttendanceDuplicateValidator
    {

        private readonly IAttendanceLogResolutionRepository
       _resolutionRepository;

        public AttendanceDuplicateValidator(
            IAttendanceLogResolutionRepository resolutionRepository)
        {
            _resolutionRepository = resolutionRepository;
        }

        public async Task<AttendanceDuplicateValidationResult>
            ValidateAsync(
                int attendanceLogId,
                CancellationToken cancellationToken = default)
        {
            if (attendanceLogId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attendanceLogId),
                    "Attendance log ID must be greater than zero.");
            }

            var existingResolution =
                await _resolutionRepository
                    .GetByAttendanceLogIdAsync(
                        attendanceLogId,
                        cancellationToken);

            if (existingResolution is null)
            {
                return AttendanceDuplicateValidationResult.Valid();
            }

            return AttendanceDuplicateValidationResult.Duplicate(
                existingResolution);
        }
    }
}
