using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public sealed class AttendancePlanResolutionResult
    {
        public long? WorkPlanId { get; set; }

        public int? WorkPlanSegmentId { get; set; }

        public int? JobId { get; set; }

        public string? SegmentName { get; set; }

        public AttendanceClockType ClockType { get; set; }

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
