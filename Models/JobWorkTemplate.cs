using HRM.Models.WorkPlanning;

namespace HRM.Models
{
    public class JobWorkTemplate
    {
        public long JobWorkTemplateId { get; set; }

        public int JobId { get; set; }

        public int WorkTemplateId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        // Higher value wins if two valid assignments overlap.
        public int Priority { get; set; }

        public bool IsActive { get; set; } = true;

        public int? OperationLogId { get; set; }

        public virtual Job Job { get; set; } = null!;

        public virtual WorkTemplate WorkTemplate { get; set; } = null!;
    }
}
