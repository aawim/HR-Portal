using HRM.Models.WorkPlanning;
using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.WorkPlanning
{
    public class UpdateWorkTemplateDto
    {
        public int WorkTemplateId { get; set; }

        public int WorkTemplateTypeId { get; set; }

        public int WorkTemplateSegmentId { get; set; }  

        public int? OrganisationBusinessEntityId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Description { get; set; }


        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }


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

        public int? OperationLogId { get; set; }

 
 

        [Required]
        public string TemplateType { get; set; } = "Regular Shift";

        //public virtual WorkTemplateType TemplateType { get; set; } = null!;

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool AllowsOverlap { get; set; }

        public bool IsValid { get; set; }
 

         
    }
}
