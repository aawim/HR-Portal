namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplateTypeDto
    {

        public int WorkTemplateTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
