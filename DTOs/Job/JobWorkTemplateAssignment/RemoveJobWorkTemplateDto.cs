using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.Job.JobWorkTemplateAssignment
{
    public sealed class RemoveJobWorkTemplateDto
    {
        [Range(1, int.MaxValue)]
        public int JobWorkTemplateAssignmentId { get; set; }

        [Range(1, int.MaxValue)]
        public int JobId { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; } =
            DateTime.Today;
    }
}
