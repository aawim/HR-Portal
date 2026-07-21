
using HRM.Enum;

namespace HRM.Models.WorkPlanning
{
    public partial class WorkPlanning
    {
        public long WorkAssignmentID { get; set; }

       public int? WorkTemplateId { get; set; }

        public int WorkTemplateTypeId { get; set; }

        public int WorkAssignmentStateId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public int GraceMinutes { get; set; }
      
        public bool RequiresCheckOut { get; set; }

        public WorkAssignmentSource AssignmentSource { get; set; }

        public string? SourceReferenceType { get; set; }

        public long? SourceReferenceId { get; set; }

        public string? LocationName { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public int? AllowedRadiusMeters { get; set; }

        public DateTime? CancelledDate { get; set; }

        public string? CancellationReason { get; set; }

        public int? CancelledByUserId { get; set; }

        public bool IsValid { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? CreatedByUserId { get; set; }

        public virtual WorkPlan WorkPlan { get; set; } = null!;

        public virtual WorkTemplate? WorkTemplate { get; set; }

        public virtual WorkTemplateType WorkTemplateType { get; set; } = null!;

        public virtual WorkAssignmentState WorkAssignmentState { get; set; } = null!;

        public virtual User? CreatedByUser { get; set; }

        public virtual User? CancelledByUser { get; set; }

        public virtual OperationLog? OperationLog { get; set; }

        public virtual ICollection<WorkAssignmentSegment> Segments { get; set; }
            = new List<WorkAssignmentSegment>();

        public virtual ICollection<WorkAssignmentOwner> Owners { get; set; }
            = new List<WorkAssignmentOwner>();

        public virtual ICollection<WorkAssignmentTransfer> Transfers { get; set; }
            = new List<WorkAssignmentTransfer>();


        public virtual ICollection<AttendanceLogResolution> AttendanceLogResolutions { get; set; }
            = new List<AttendanceLogResolution>();

    }
}
