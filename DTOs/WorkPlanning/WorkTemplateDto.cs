using HRM.Models.WorkPlanning;

namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplateDto
    {

        public int WorkTemplateId { get; set; }

        public int WorkTemplateTypeId { get; set; }

        public string WorkTemplateTypeName { get; set; } = string.Empty;

        public int? OrganisationBusinessEntityId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Description { get; set; }

        public TimeOnly? DefaultStartTime { get; set; }

        public TimeOnly? DefaultEndTime { get; set; }

        public bool EndsNextDay { get; set; }

        public int DefaultGraceMinutes { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresCheckOut { get; set; }

        public bool IsRepeatable { get; set; }

        public bool IsGlobal { get; set; }

        public bool IsActive { get; set; }

        public DateOnly? EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public int SegmentCount { get; set; }

        public string TemplateType { get; set; } = "RegularShift";

        //public virtual WorkTemplateType TemplateType { get; set; } = null!;

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool AllowsOverlap { get; set; }

        public bool IsValid { get; set; }

    
    }
}
