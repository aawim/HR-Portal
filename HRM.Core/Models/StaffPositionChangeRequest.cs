
namespace HRM.Models;

public sealed class StaffPositionChangeRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public int PositionId { get; set; }
    public string ChangeKind { get; set; } = "Assign";
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? OriginalJson { get; set; }
    public string Reason { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
}
