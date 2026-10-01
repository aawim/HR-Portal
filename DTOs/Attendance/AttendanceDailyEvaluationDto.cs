using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public sealed class AttendanceDailyEvaluationDto
    {
        // =========================================================
        // Identity
        // =========================================================

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public int OrganisationId { get; set; }

        public DateTime WorkDate { get; set; }


        // =========================================================
        // Work Plan
        // =========================================================

        public long? WorkPlanId { get; set; }

        public bool HasWorkPlan { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresCheckOut { get; set; }


        // =========================================================
        // Planned
        // =========================================================

        public DateTime? PlannedStart { get; set; }

        public DateTime? PlannedEnd { get; set; }

        public int PlannedMinutes { get; set; }


        // =========================================================
        // Actual
        // =========================================================

        public DateTime? FirstCheckIn { get; set; }

        public DateTime? LastCheckOut { get; set; }

        public int WorkedMinutes { get; set; }


        // =========================================================
        // Variance
        // =========================================================

        public int LateMinutes { get; set; }

        public int EarlyDepartureMinutes { get; set; }

        public int OvertimeMinutes { get; set; }


        // =========================================================
        // Clock Statistics
        // =========================================================

        public int RawClockCount { get; set; }

        public int ResolvedClockCount { get; set; }

        public int UnresolvedClockCount { get; set; }


        // =========================================================
        // Result
        // =========================================================

        public AttendanceDailyStatus Status { get; set; }

        public bool IsComplete { get; set; }

        public bool HasException { get; set; }


        // =========================================================
        // Details
        // =========================================================

        public List<AttendanceEvaluationIssueDto> Issues { get; set; } = [];

        public List<AttendanceEvaluatedSegmentDto> Segments { get; set; } = [];
    }
}
