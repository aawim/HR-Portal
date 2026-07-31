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

 

        public string? TemplateCode { get; set; }

        public string TemplateTypeName { get; set; } =
            string.Empty;

     

        public TimeOnly? DefaultStartTime { get; set; }

        public TimeOnly? DefaultEndTime { get; set; }

        public bool EndsNextDay { get; set; }
    }
}
