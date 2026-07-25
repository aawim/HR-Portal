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
    }
}
