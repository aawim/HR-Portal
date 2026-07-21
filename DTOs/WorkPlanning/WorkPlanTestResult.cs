namespace HRM.DTOs.WorkPlanning
{
    public class WorkPlanTestResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public long? WorkPlanId { get; set; }

        public long? WorkAssignmentId { get; set; }

        public string? TemplateName { get; set; }

        public string? AssignmentTitle { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public int SegmentCount { get; set; }

        public int OwnerCount { get; set; }
    }
}
