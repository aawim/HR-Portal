namespace HRM.DTOs.StaffSchedule
{
    public sealed class ManualWorkAssignmentListDto
    {
        public long WorkPlanId { get; set; }

        public long WorkAssignmentId { get; set; }

        public DateTime WorkDate { get; set; }

        public string AssignmentName { get; set; } =
            string.Empty;

        public string WorkTemplateName { get; set; } =
            string.Empty;

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public bool IsFinalized { get; set; }

        public bool IsValid { get; set; }

        public string? Remarks { get; set; }

        public string Status =>
            !IsValid
                ? "Removed"
                : IsFinalized
                    ? "Finalized"
                    : "Active";
    }
}
