using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public sealed class AttendanceEvaluationIssueDto
    {
        public AttendanceEvaluationIssueType Type { get; set; }

        public AttendanceEvaluationIssueSeverity Severity { get; set; }

        public AttendanceExceptionType? ExceptionType { get; set; }

        public string Message { get; set; } = string.Empty;


        // Related WorkPlan information

        public long? WorkPlanId { get; set; }

        public long? WorkAssignmentId { get; set; }

        public int? WorkPlanSegmentId { get; set; }


        // Related physical clock event

        public int? AttendanceLogId { get; set; }


        // Evaluation details

        public DateTime? ExpectedTime { get; set; }

        public DateTime? ActualTime { get; set; }

        public int? DifferenceMinutes { get; set; }
    }
}
