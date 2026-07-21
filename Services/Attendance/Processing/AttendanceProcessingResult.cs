using HRM.Constants;
using HRM.Models;

namespace HRM.Services.Attendance.Processing
{
    public sealed class AttendanceProcessingResult
    {
        public bool Success { get; init; }

        public bool IsResolved { get; init; }

        public bool AlreadyProcessed { get; init; }

        public int AttendanceLogId { get; init; }

        public long? AttendanceLogResolutionId { get; init; }

        public int ResolutionStatusId { get; init; }

        public string Message { get; init; } = string.Empty;

        public AttendanceLogResolution? Resolution { get; init; }

        public static AttendanceProcessingResult Completed(
            AttendanceLogResolution resolution,
            string message)
        {
            return new AttendanceProcessingResult
            {
                Success = true,
                IsResolved = true,
                AttendanceLogId = resolution.AttendanceLogId,
                AttendanceLogResolutionId =
                    resolution.AttendanceLogResolutionId,
                ResolutionStatusId =
                    resolution.AttendanceResolutionStatusId,
                Message = message,
                Resolution = resolution
            };
        }

        public static AttendanceProcessingResult Recorded(
            AttendanceLogResolution resolution,
            string message)
        {
            return new AttendanceProcessingResult
            {
                Success = true,
                IsResolved = false,
                AttendanceLogId = resolution.AttendanceLogId,
                AttendanceLogResolutionId =
                    resolution.AttendanceLogResolutionId,
                ResolutionStatusId =
                    resolution.AttendanceResolutionStatusId,
                Message = message,
                Resolution = resolution
            };
        }

        public static AttendanceProcessingResult AlreadyExists(
            AttendanceLogResolution resolution)
        {
            return new AttendanceProcessingResult
            {
                Success = true,
                IsResolved =
                    resolution.AttendanceResolutionStatusId ==
                    AttendanceResolutionStatusIds.Resolved,
                AlreadyProcessed = true,
                AttendanceLogId = resolution.AttendanceLogId,
                AttendanceLogResolutionId =
                    resolution.AttendanceLogResolutionId,
                ResolutionStatusId =
                    resolution.AttendanceResolutionStatusId,
                Message = "The attendance log has already been processed.",
                Resolution = resolution
            };
        }

        public static AttendanceProcessingResult Failed(
            int attendanceLogId,
            string message)
        {
            return new AttendanceProcessingResult
            {
                Success = false,
                IsResolved = false,
                AttendanceLogId = attendanceLogId,
                Message = message
            };
        }
    }
}
