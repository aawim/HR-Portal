using HRM.Enum;
using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.WorkPlanning
{
    public class GenerateWorkPlanDto : IValidatableObject
    {
        [Range(1, int.MaxValue, ErrorMessage = "Individual is required.")]
        public int IndividualId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Job is required.")]
        public int JobId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Organisation is required.")]
        public int OrganisationBusinessEntityId { get; set; }

        public int WorkTemplateId { get; set; }

        public int? PlanningProviderId { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((IsManual || WorkPlanGenerationSource == HRM.Enum.WorkPlanGenerationSource.Manual) && WorkTemplateId <= 0)
                yield return new ValidationResult("Choose a template for the manual assignment.", [nameof(WorkTemplateId)]);
            if (WorkDate == default) yield return new ValidationResult("Choose a work date.", [nameof(WorkDate)]);
        }

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
