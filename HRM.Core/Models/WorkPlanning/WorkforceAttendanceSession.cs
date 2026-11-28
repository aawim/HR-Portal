namespace HRM.Models.WorkPlanning;
/// <summary>Versioned evaluation result for a planned duty session; evidence remains immutable.</summary>
public sealed class WorkforceAttendanceSession
{
    public long Id { get; set; }
    public int WorkPlanSegmentId { get; set; }
    public long? DutyAssignmentId { get; set; }
    public string Status { get; set; } = "PendingReview";
    public string EvaluationStrategy { get; set; } = "CheckInOut";
    public int EvaluationVersion { get; set; } = 1;
    public int ActualMinutes { get; set; }
    public int CreditedMinutes { get; set; }
    public string? MatchReason { get; set; }
    public DateTime EvaluatedAtUtc { get; set; }
    public int OperationLogId { get; set; }
}
