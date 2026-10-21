namespace HRM.Models.WorkPlanning;

public sealed class DutyChangeRequest
{
    public long DutyChangeRequestId { get; set; }
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;
    public int OrganisationId { get; set; }
    public string Kind { get; set; } = "Transfer";
    public long SourceAssignmentId { get; set; }
    public long? SwapAssignmentId { get; set; }
    public int SourceJobId { get; set; }
    public int TargetJobId { get; set; }
    public int SourceIndividualId { get; set; }
    public int TargetIndividualId { get; set; }
    public DateTime SourceStart { get; set; }
    public DateTime SourceEnd { get; set; }
    public DateTime? SwapStart { get; set; }
    public DateTime? SwapEnd { get; set; }
    public string Reason { get; set; } = "";
    public string SourceFingerprint { get; set; } = "";
    public string? SwapFingerprint { get; set; }
    public string Acceptance { get; set; } = "Pending";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? AcceptedByUserId { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public int? SourceAcceptedByUserId { get; set; }
    public DateTime? SourceAcceptedAtUtc { get; set; }
    public bool IsOpen { get; set; } = true;
    public long? TargetNewAssignmentId { get; set; }
    public long? SourceNewAssignmentId { get; set; }
}
