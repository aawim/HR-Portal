using HRM.Models;

namespace HRM.Services.Attendance.Validation
{
    public sealed class AttendanceDuplicateValidationResult
    {
        public bool IsDuplicate { get; init; }

        public string Message { get; init; } = string.Empty;

        public AttendanceLogResolution? ExistingResolution { get; init; }

        public static AttendanceDuplicateValidationResult Valid()
        {
            return new AttendanceDuplicateValidationResult
            {
                IsDuplicate = false
            };
        }

        public static AttendanceDuplicateValidationResult Duplicate(
            AttendanceLogResolution existingResolution)
        {
            return new AttendanceDuplicateValidationResult
            {
                IsDuplicate = true,
                ExistingResolution = existingResolution,
                Message = "The attendance log has already been processed."
            };
        }
    }
}

