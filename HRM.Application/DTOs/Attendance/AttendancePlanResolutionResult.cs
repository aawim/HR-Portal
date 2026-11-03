using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public sealed class AttendancePlanResolutionResult
    {
        public long? WorkPlanId { get; set; }

        public long? WorkAssignmentId { get; set; }

        public int? WorkPlanSegmentId { get; set; }

        public int? JobId { get; set; }

        public string? SchedulePurpose { get; set; }

        public string? SegmentName { get; set; }

        public AttendanceClockType ClockType { get; set; }

        public string? LogicalEventType { get; set; }

        public string? MatchMethod { get; set; }

        public string? MatchReason { get; set; }

        public string ConfidenceState { get; set; } = "Deterministic";

        public long? SecondaryWorkAssignmentId { get; set; }

        public int? SecondaryWorkPlanSegmentId { get; set; }

        public string? SecondarySchedulePurpose { get; set; }

        public string? SecondarySegmentName { get; set; }

        public AttendanceClockType SecondaryClockType { get; set; } = AttendanceClockType.Unresolved;

        public string? SecondaryLogicalEventType { get; set; }

        public string? SecondaryMatchMethod { get; set; }

        public string? SecondaryMatchReason { get; set; }

        public List<AttendanceResolvedAssociationDto> AdditionalAssociations { get; set; } = [];

        public AttendancePlanResolutionState State { get; set; }

        public DateTime? BoundaryTime { get; set; }
        public double DistanceMinutes { get; set; }

        public bool IsResolved =>
            WorkPlanId.HasValue &&
            WorkPlanSegmentId.HasValue &&
            ClockType != AttendanceClockType.Unresolved;

        public string Message { get; set; } = string.Empty;
    }
}
