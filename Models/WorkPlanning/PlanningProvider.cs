namespace HRM.Models.WorkPlanning
{
    public class PlanningProvider
    {
        public int PlanningProviderId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<OrganisationWorkPlanningSetting>
            OrganisationWorkPlanningSettings
        { get; set; }
            = new List<OrganisationWorkPlanningSetting>();

        public virtual ICollection<WorkPlan> WorkPlans { get; set; } = new List<WorkPlan>();
    }
}
