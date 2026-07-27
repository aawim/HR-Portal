namespace HRM.Models.WorkPlanning
{
    public class JobWorkTemplateAssignment
    {
        public int JobWorkTemplateAssignmentId { get; set; }

        public int JobId { get; set; }

        public int WorkTemplateId { get; set; }

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public Job Job { get; set; } = null!;

        public WorkTemplate WorkTemplate { get; set; } = null!;
    }
}
