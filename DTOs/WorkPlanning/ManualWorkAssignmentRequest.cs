namespace HRM.DTOs.WorkPlanning
{
    public class ManualWorkAssignmentRequest
    {
        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int WorkTemplateId { get; set; }

        public int? PlanningProviderId { get; set; }

        public DateTime WorkDate { get; set; } =
            DateTime.Today;

        public string? AssignmentTitle { get; set; }

        public string? AssignmentDescription { get; set; }

        public string? Remarks { get; set; }

        public bool RequiresAttendance { get; set; } =
            true;

        public bool RequiresCheckout { get; set; } =
            true;

        public int Priority { get; set; }

        public string AssignmentSource { get; set; } =
            "Manual";
    }
}
