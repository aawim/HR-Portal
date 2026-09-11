using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.Job.JobWorkTemplateAssignment
{
    public sealed class AssignJobWorkTemplateDto
    {
        [Range(1, int.MaxValue)]
        public int JobId { get; set; }

        [Range(1, int.MaxValue)]
        public int IndividualId { get; set; }

        [Range(1, int.MaxValue)]
        public int OrganisationBusinessEntityId { get; set; }

        [Range(1, int.MaxValue)]
        public int WorkTemplateId { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; } =
            DateTime.Today;

        public DateTime? EffectiveTo { get; set; }
 
        public bool Monday { get; set; } = true;
        public bool Tuesday { get; set; } = true;
        public bool Wednesday { get; set; } = true;
        public bool Thursday { get; set; } = true;
        public bool Friday { get; set; } = true;
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
        public int Priority { get; set; }
        public int? OperationLogId { get; set; }
    }
}
