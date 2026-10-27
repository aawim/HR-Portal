
namespace HRM.Models;

// Proposed work and missing punches remain separate from payable claims.
public sealed class OvertimeWorkRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public string Kind { get; set; } = "Preapproval";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime? ProposedStart { get; set; }
    public DateTime? ProposedEnd { get; set; }
    public int? StartLogId { get; set; }
    public int? EndLogId { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
    public ICollection<OvertimeWorkTask> Tasks { get; set; } = new List<OvertimeWorkTask>();
}

public sealed class OvertimeWorkTask
{
    public int Id { get; set; }
    public int WorkRequestId { get; set; }
    public string Description { get; set; } = "";
    public OvertimeWorkRequest WorkRequest { get; set; } = null!;
}
