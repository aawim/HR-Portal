
namespace HRM.Models;

public sealed class StaffOnboardingRequest
{
    public int RequestId { get; set; }
    public int IndividualId { get; set; }
    public int OrganisationId { get; set; }
    public int OrganisationStructureId { get; set; }
    public int JobTypeId { get; set; }
    public int? PositionId { get; set; }
    public DateTime JoinedDate { get; set; }
    public string Remarks { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsOpen { get; set; } = true;
    public int? JobId { get; set; }
    public int? UserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Request Request { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
}
