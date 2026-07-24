using HRM.Models.WorkPlanning;

namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplatePresetDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string TemplateType { get; set; } = "Regular Shift";

        public int WorkTemplateTypeId { get; set; }

        //public virtual WorkTemplateType? TemplateType { get; set; } = null!;

        public TimeSpan? DefaultStartTime { get; set; }

        public TimeSpan? DefaultEndTime { get; set; }

        public bool RequiresAttendance { get; set; } = true;

        public bool RequiresCheckOut { get; set; } = true;

        public bool AllowsOverlap { get; set; }

        public string Icon { get; set; } = "📋";

        public bool IsActive { get; set; } 
    }
}
