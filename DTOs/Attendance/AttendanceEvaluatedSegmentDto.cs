namespace HRM.DTOs.Attendance
{
    public sealed class AttendanceEvaluatedSegmentDto
    {
        public int WorkPlanSegmentId { get; set; }

        public long WorkPlanId { get; set; }

        public int WorkSegmentTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int SequenceNumber { get; set; }


        // Requirement

        public bool IsMandatory { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool IsPaid { get; set; }


        // Planned

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public int GraceBeforeMinutes { get; set; }

        public int GraceAfterMinutes { get; set; }

        public int PlannedMinutes { get; set; }


        // Actual

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        public int WorkedMinutes { get; set; }


        // Variance

        public int LateMinutes { get; set; }

        public int EarlyDepartureMinutes { get; set; }


        // Result

        public bool IsComplete { get; set; }

        public bool HasException { get; set; }


        // Segment-specific problems

        public List<AttendanceEvaluationIssueDto> Issues { get; set; } = [];
    }
}