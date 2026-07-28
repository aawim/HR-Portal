namespace HRM.Models.WorkPlanning
{
    public class JobWorkTemplateAssignment
    {
        public int JobWorkTemplateAssignmentId { get; set; }

        public int JobId { get; set; }

        public int WorkTemplateId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public Job Job { get; set; } = null!;

        public WorkTemplate WorkTemplate { get; set; } = null!;


        public int JobWorkTemplateAssignmentID { get; set; }

        public int JobID { get; set; }

        public int WorkTemplateID { get; set; }

        // Recommended for generation
        public TimeOnly? ScheduledStartTime { get; set; }

 
    }
}
