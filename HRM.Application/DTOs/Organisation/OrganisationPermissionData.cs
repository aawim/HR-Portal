using HRM.DTOs.Roles;

namespace HRM.DTOs.Organisation;

public sealed class OrganisationPermissionData
{
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public bool IsSuperAdmin { get; set; }
    public bool CanEdit { get; set; }
    public int AccountCount { get; set; }
    public List<PermissionSettingOption> Organisations { get; set; } = [];
    public List<PermissionDefinitionDto> Permissions { get; set; } = [];
    public List<int> AllowedIds { get; set; } = [];
    public List<int> AssignableIds { get; set; } = [];
}
