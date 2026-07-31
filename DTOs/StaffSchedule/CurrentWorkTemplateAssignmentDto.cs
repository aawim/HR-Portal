namespace HRM.DTOs.StaffSchedule
{
    public sealed class CurrentWorkTemplateAssignmentDto
    {
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
