namespace HRM.DTOs.WorkPlanning
{
    public sealed class AssignedWorkTemplateDto
    {
        public int JobWorkTemplateAssignmentId { get; set; }

        public int WorkTemplateId { get; set; }

        public string TemplateName { get; set; } =
            string.Empty;

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
    }
}
