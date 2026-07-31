using HRM.Models.WorkPlanning;

namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplateLookupDto
    {
        public int WorkTemplateId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public string? TemplateType { get; set; }

        public string? Code { get; set; }

        public string TemplateTypeName { get; set; } = string.Empty;

        // Add these
        public TimeOnly? DefaultStartTime { get; set; }

        public TimeOnly? DefaultEndTime { get; set; }

        public bool EndsNextDay { get; set; }
    }
}
