using HRM.Enum;
using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.WorkPlanning
{
    public class GenerateWorkPlanDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Individual is required.")]
        public int IndividualId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Job is required.")]
        public int JobId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Organisation is required.")]
        public int OrganisationBusinessEntityId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Work template is required.")]
        public int WorkTemplateId { get; set; }

        [Required(ErrorMessage = "Planning provider is required.")]
        public int? PlanningProviderId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        public string? Remarks { get; set; }

        public bool IsManual { get; set; }

        public int GeneratedByUserId { get; set; }

        public WorkPlanGenerationSource? WorkPlanGenerationSource { get; set; }

        public int WorkPlanId { get; set; }

        public int OperationLogId { get; set; }
    }
}
