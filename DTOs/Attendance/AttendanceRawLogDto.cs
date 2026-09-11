using HRM.Constants;
using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public class AttendanceRawLogDto
    {
        // Raw AttendanceLog
        public int AttendanceLogId { get; set; }
        public int IndividualId { get; set; }
        public DateTime LogDateTime { get; set; }

        public int InOutModeId { get; set; }
 
        // What this fingerprint/clock event means
        public AttendanceClockType ClockType { get; set; } = AttendanceClockType.Unresolved;

        // Problem detected with this event/segment
        public AttendanceExceptionType ExceptionType { get; set; }






        // Persisted resolution
        public long? AttendanceLogResolutionId { get; set; }

        public long? WorkPlanId { get; set; }
        public int? WorkPlanSegmentId { get; set; }

        public string? SegmentName { get; set; }

     

        public int? ResolutionStatusId { get; set; }

        public string? ResolutionMessage { get; set; }

        public DateTime? ResolutionDate { get; set; }

        public bool IsResolved =>
            ResolutionStatusId ==
                AttendanceResolutionStatusIds.Resolved;
    }
}
