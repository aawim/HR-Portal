namespace HRM.Models.WorkPlanning
{
    public class JobWorkTemplateAssignment
    {
        public string SchedulePurpose { get; set; } = "Official";
        public string ScheduleMode { get; set; } = "Standard";
        public int WorkingDaysMask { get; set; } = 31;
        public DateTime? RotationAnchorDate { get; set; }
        public string? RotationTemplateIds { get; set; }
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
