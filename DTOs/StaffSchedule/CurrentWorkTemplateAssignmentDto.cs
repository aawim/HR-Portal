namespace HRM.DTOs.StaffSchedule
{
    public sealed class CurrentWorkTemplateAssignmentDto
    {
        public string SchedulePurpose { get; set; } = "Official";
        public string ScheduleMode { get; set; } = "Standard";
        public int WorkingDaysMask { get; set; } = 31;
        public DateTime? RotationAnchorDate { get; set; }
        public string? RotationTemplateIds { get; set; }
        public bool EndsNextDay { get; set; }
        public List<int> MandatoryCheckInOffsets { get; set; } = [];
        public List<int> MandatoryCheckOutOffsets { get; set; } = [];
        public int JobWorkTemplateAssignmentId { get; set; }

        public int JobId { get; set; }

        public int WorkTemplateId { get; set; }

        public string TemplateName { get; set; } =
            string.Empty;

        public string? TemplateCode { get; set; }

        public string TemplateTypeName { get; set; } =
            string.Empty;

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public DateTime? DefaultStartTime { get; set; }

        public DateTime? DefaultEndTime { get; set; }
    }
}
