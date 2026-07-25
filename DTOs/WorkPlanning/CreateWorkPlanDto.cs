using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.WorkPlanning
{
    public class CreateWorkPlanDto
    {
        [Range(1, int.MaxValue)]
        public int IndividualId { get; set; }

        [Range(1, int.MaxValue)]
        public int JobId { get; set; }

        [Range(1, int.MaxValue)]
        public int OrganisationBusinessEntityId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a work template.")]
        public int WorkTemplateId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        public int? PlanningProviderId { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsManual { get; set; }
    }
}
