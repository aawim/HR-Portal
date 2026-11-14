namespace HRM.Models.WorkPlanning;

/// <summary>Auditable dynamic change to a duty; the original assignment remains intact.</summary>
public sealed class DutyOperation
{
    public long Id { get; set; }
    public long DutyAssignmentId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = "Requested";
    public long? ReplacementDutyAssignmentId { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public int? RequestId { get; set; }
    public int OperationLogId { get; set; }
    public DutyAssignment DutyAssignment { get; set; } = null!;
    public Request? Request { get; set; }
}
