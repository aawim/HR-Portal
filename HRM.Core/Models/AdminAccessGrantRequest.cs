
namespace HRM.Models;

public sealed class AdminAccessGrantRequest
{
    public int RequestId { get; set; }
    public int UserId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int CreatedByUserId { get; set; }
    public string GroupIdsJson { get; set; } = "[]";
    public string PermissionIdsJson { get; set; } = "[]";
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public bool IsOpen { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Request Request { get; set; } = null!;
}
