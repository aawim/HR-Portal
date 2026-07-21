namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkAssignmentFilterDto
    {
        public DateTime FromDate { get; set; } = DateTime.Today;

        public DateTime ToDate { get; set; } =
            DateTime.Today.AddDays(7);

        public int? OrganisationStructureId { get; set; }

        public int? WorkPlanId { get; set; }

        public string? AssignmentStatus { get; set; }

        public string? SearchText { get; set; }

    }
}
