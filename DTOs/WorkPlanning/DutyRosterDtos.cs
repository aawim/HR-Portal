namespace HRM.DTOs.WorkPlanning;

public sealed record DutyRosterRow(long AssignmentId, int JobId, int IndividualId, string Staff, string Department,
    string Duty, DateTime Start, DateTime End, bool RequiresAttendance, bool RequiresCheckout, bool CanRequest);
public sealed record DutyPartner(int JobId, int IndividualId, string Name);
public sealed record DutyChangeApplication(long SourceAssignmentId, int TargetJobId, long? SwapAssignmentId, string Reason);
public sealed class DutyRequestRow
{
    public long Id { get; set; }
    public int RequestId { get; set; }
    public int StateId { get; set; }
    public string Reference { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public DateTime? SwapStart { get; set; }
    public DateTime? SwapEnd { get; set; }
    public string Acceptance { get; set; } = "";
    public string SourceAcceptance { get; set; } = "Pending";
    public string TargetAcceptance { get; set; } = "Pending";
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public string? Stage { get; set; }
    public string? Approvers { get; set; }
    public bool CanAccept { get; set; }
    public bool CanApprove { get; set; }
    public bool CanCancel { get; set; }
}
