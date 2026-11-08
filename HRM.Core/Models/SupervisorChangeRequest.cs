
namespace HRM.Models;
public sealed class SupervisorChangeRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public string TargetKind { get; set; } = "Department";
    public int TargetId { get; set; }
    public string TargetName { get; set; } = "";
    public bool IsAssignment { get; set; }
    public bool IsOpen { get; set; } = true;
    public string OriginalVersion { get; set; } = "";
    public string Reason { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
}
