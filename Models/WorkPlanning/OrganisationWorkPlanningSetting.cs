namespace HRM.Models.WorkPlanning
{
    public class OrganisationWorkPlanningSetting
    {
        public int OrganisationWorkPlanningSettingId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int PlanningProviderId { get; set; }

        public DateOnly? EffectiveFromDate { get; set; }

        public DateOnly? EffectiveToDate { get; set; }

        public bool IsActive { get; set; }

        public int? OperationLogId { get; set; }

        public virtual Organisation Organisation { get; set; } = null!;

        public virtual PlanningProvider PlanningProvider { get; set; } = null!;

        public virtual OperationLog? OperationLog { get; set; }
    }
}
