namespace HRM.DTOs.Job
{
    public class JobWorkTemplateDto
    {
        public long JobWorkTemplateId { get; set; }

        public int JobId { get; set; }

        public long WorkTemplateId { get; set; }

        public string WorkTemplateName { get; set; } = string.Empty;

        public string? WorkTemplateCode { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        public int Priority { get; set; }

        public bool IsActive { get; set; }
    }
}
