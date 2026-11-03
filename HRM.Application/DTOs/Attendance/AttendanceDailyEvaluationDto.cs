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

        public int PlannedOfficialMinutes { get; set; }

        public int PlannedDutyMinutes { get; set; }


        // =========================================================
        // Actual & Credited (Official vs Duty Separated)
        // =========================================================

        public DateTime? FirstCheckIn { get; set; }

        public DateTime? LastCheckOut { get; set; }

        public DateTime? OfficialBreakOut { get; set; }

        public DateTime? OfficialBreakIn { get; set; }

        public int WorkedMinutes { get; set; }

        public int ActualOfficialPresenceMinutes { get; set; }

        public int CreditedOfficialMinutes { get; set; }

        public int OfficialBreakMinutes { get; set; }

        public int ExcessBreakMinutes { get; set; }

        public int LateBreakReturnMinutes { get; set; }

        public int ActualDutyPresenceMinutes { get; set; }

        public int CreditedDutyMinutes { get; set; }

        public int TotalPhysicalPresenceMinutes { get; set; }

        public int TotalCreditedPayableMinutes { get; set; }


        // =========================================================
        // Variance
        // =========================================================

        public int LateMinutes { get; set; }

        public int EarlyDepartureMinutes { get; set; }

        public int OvertimeMinutes { get; set; }
        // Candidate minutes only; an OT request and approval are still required.
        public int PotentialOvertimeMinutes { get; set; }
        public int DutyWithinOfficialMinutes { get; set; }
        public int PlannedDutyOutsideOfficialMinutes { get; set; }


        // =========================================================
        // Clock Statistics
        // =========================================================

        public int RawClockCount { get; set; }

        public int ResolvedClockCount { get; set; }

        public int UnresolvedClockCount { get; set; }

        public int SharedEventCount { get; set; }

        public int DuplicateIgnoredCount { get; set; }

        public int ConflictCount { get; set; }


        // =========================================================
        // Result
        // =========================================================

        public AttendanceDailyStatus Status { get; set; }

        public AttendanceObligationState OfficialState { get; set; } = AttendanceObligationState.NotStarted;

        public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";

        public bool IsComplete { get; set; }

        public bool HasException { get; set; }


        // =========================================================
        // Details
        // =========================================================

        public List<AttendanceEvaluationIssueDto> Issues { get; set; } = [];

        public List<AttendanceEvaluatedSegmentDto> Segments { get; set; } = [];

        public List<AttendanceObligationSummaryDto> Obligations { get; set; } = [];

        public List<AttendanceEventInterpretationDto> EventInterpretations { get; set; } = [];
    }
}
