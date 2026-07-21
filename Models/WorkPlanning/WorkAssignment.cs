using HRM.Enum;

namespace HRM.Models.WorkPlanning;

public partial class WorkAssignment
{
    public long WorkAssignmentId { get; set; }

    public long WorkPlanId { get; set; }

    public int WorkTemplateId { get; set; }

    public int WorkTemplateTypeId { get; set; }

    public int WorkAssignmentStateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public int GraceMinutes { get; set; }

    public bool RequiresAttendance { get; set; }

    public bool RequiresCheckOut { get; set; }

    public int Priority { get; set; }

    public WorkAssignmentSource AssignmentSource { get; set; }

    public int? SourceReferenceType { get; set; }

    public long? SourceReferenceId { get; set; }

    public string? LocationName { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public decimal? AllowedRadiusMeters { get; set; }

    public DateTime? CancelledDate { get; set; }

    public string? CancellationReason { get; set; }

    public int? CancelledByUserId { get; set; }

    public bool IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? CreatedByUserId { get; set; }

    public virtual WorkPlan WorkPlan { get; set; } = null!;

    public virtual WorkTemplate WorkTemplate { get; set; } = null!;

    public virtual WorkTemplateType WorkTemplateType { get; set; } = null!;

    public virtual WorkAssignmentState WorkAssignmentState { get; set; }
        = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual User? CancelledByUser { get; set; }

    public virtual ICollection<WorkAssignmentSegment>
        WorkAssignmentSegments
    { get; set; }
            = new List<WorkAssignmentSegment>();

    public virtual ICollection<WorkAssignmentOwner>
        WorkAssignmentOwners
    { get; set; }
            = new List<WorkAssignmentOwner>();

    public virtual ICollection<AttendanceLogResolution> AttendanceLogResolutions { get; set; }
        = new List<AttendanceLogResolution>();
}