namespace HRM.DTOs.StaffSchedule
{
    public sealed class JobWorkTemplateAssignmentHistoryDto
    {
        public int JobWorkTemplateAssignmentId { get; set; }

        public int WorkTemplateId { get; set; }

        public string TemplateName { get; set; } =
            string.Empty;

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }
    }
}
