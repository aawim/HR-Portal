namespace HRM.Models.WorkPlanning
{
    public class WorkTemplateType
    {
        public int WorkTemplateTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<WorkTemplate> WorkTemplates { get; set; }
            = new List<WorkTemplate>();

        public virtual ICollection<WorkAssignment> WorkAssignments { get; set; } = new List<WorkAssignment>();
    }
}
