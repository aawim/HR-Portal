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
        public string? SchedulePurpose { get; set; }
        public long? WorkAssignmentId { get; set; }
        public string? ScheduleName { get; set; }
        public DateTime? ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyDepartureMinutes { get; set; }
        public bool IsPrimaryClassification { get; set; } = true;
        public string? LogicalEventType { get; set; }
        public string? MatchMethod { get; set; }
        public string? MatchReason { get; set; }
        public string ConfidenceState { get; set; } = "Deterministic";
        public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";
        public bool IsSharedAcrossSchedules { get; set; }
        public string? SharedScheduleSummary { get; set; }
        public List<AttendanceEventAssociationDto> Associations { get; set; } = [];

        public string ClockLabel => SchedulePurpose == "Duty"
            ? ClockType == AttendanceClockType.CheckIn ? LateMinutes > 0 ? $"Duty check-in · Late by {LateMinutes} minutes" : "Duty check-in"
                : ClockType == AttendanceClockType.CheckOut ? "Duty check-out" : ClockType.ToString()
            : SchedulePurpose == "Official" && ClockType == AttendanceClockType.CheckIn ? "Official check-in"
            : SchedulePurpose == "Official" && ClockType == AttendanceClockType.CheckOut ? "Official check-out"
            : SchedulePurpose == "Official" && ClockType == AttendanceClockType.BreakOut ? "Official lunch / break out"
            : SchedulePurpose == "Official" && ClockType == AttendanceClockType.BreakIn ? "Official lunch / break in"
            : ClockType == AttendanceClockType.CheckIn ? "Check In"
            : ClockType == AttendanceClockType.CheckOut ? "Check Out"
            : ClockType == AttendanceClockType.BreakOut ? "Break Out"
            : ClockType == AttendanceClockType.BreakIn ? "Break In"
            : ClockType.ToString();
 
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
