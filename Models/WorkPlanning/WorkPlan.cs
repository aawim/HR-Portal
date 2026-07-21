using HRM.Enum;

namespace HRM.Models.WorkPlanning
{
    public partial class WorkPlan
    {
        public long WorkPlanId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public int PlanningProviderId { get; set; }

        public int? WorkTemplateId { get; set; }

        public DateOnly? WorkDate { get; set; }

        public WorkPlanGenerationSource GenerationSource { get; set; }

        public DateTime GeneratedDate { get; set; }

        public int? GeneratedByUserId { get; set; }

        public bool IsFinalized { get; set; }

        public DateTime? FinalizedDate { get; set; }

        public int? FinalizedByUserId { get; set; }

        public string? Remarks { get; set; }

        public bool IsValid { get; set; }

        public int? OperationLogId { get; set; }

        public DateTime CreatedDate { get; set; }

        public Guid PlanGuid { get; set; }

        public int Version { get; set; }

        public bool IsGenerated { get; set; }

        public bool IsManual { get; set; }

        public virtual Individual Individual { get; set; } = null!;

        public virtual Job Job { get; set; } = null!;

        public virtual Organisation Organisation { get; set; } = null!;

        public virtual PlanningProvider PlanningProvider { get; set; } = null!;

        public virtual WorkTemplate? WorkTemplate { get; set; }

        public virtual User? GeneratedByUser { get; set; }

        public virtual User? FinalizedByUser { get; set; }

        public virtual OperationLog? OperationLog { get; set; }

        public virtual ICollection<WorkAssignment> WorkAssignments { get; set; }
            = new List<WorkAssignment>();

        public virtual ICollection<AttendanceLogResolution>AttendanceLogResolutions{ get; set; }
            = new List<AttendanceLogResolution>();


    }


}
