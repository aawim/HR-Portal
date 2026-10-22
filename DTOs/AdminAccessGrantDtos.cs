namespace HRM.DTOs;

public record AdminAccessOption(int Id, string Name)
{
    public List<int> PermissionIds { get; init; } = [];
}
public sealed class AdminAccessGrantData
{
    public bool IsSuperAdmin { get; set; }
    public bool CanManage { get; set; }
    public int OrganisationId { get; set; }
    public List<AdminAccessOption> Organisations { get; set; } = [];
    public List<AdminAccessAccount> Accounts { get; set; } = [];
    public List<AdminAccessOption> Groups { get; set; } = [];
    public List<AdminAccessOption> Permissions { get; set; } = [];
    public List<int> OrganisationPermissionIds { get; set; } = [];
    public List<AdminAccessGrantRow> Requests { get; set; } = [];
}
public sealed class AdminAccessAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public List<int> GroupIds { get; set; } = [];
    public List<int> PermissionIds { get; set; } = [];
}
public sealed class AdminAccessGrantRow
{
    public int Id { get; set; }
    public int StateId { get; set; }
    public string Reference { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Stage { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Assignments { get; set; } = "";
    public bool CanApprove { get; set; }
    public bool CanCancel { get; set; }
    public List<string> History { get; set; } = [];
}
