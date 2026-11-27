namespace HRM.DTOs.Roles;

public class PermissionDefinitionDto
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ContextId { get; set; } = 1;
    public string ContextName { get; set; } = "";
    public int? ModuleId { get; set; }
    public string ModuleName { get; set; } = "";
    public bool IsSystem { get; set; }
    public string Group => string.IsNullOrWhiteSpace(ModuleName) ? PermissionArea(Key) : ModuleName;
    public static string PermissionArea(string key)
    {
        if (key.Contains("Leave", StringComparison.OrdinalIgnoreCase)) return "Leave";
        var prefix = key.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "General";
        return prefix switch
        {
            "Roles" => "Permissions", "UserGroups" => "Roles", "Users" or "User" => "Users",
            "Admin" or "Developer" => "Administration", _ => prefix
        };
    }
}

public class AccessRoleDto
{
    public int Id { get; set; }
    public int? OrganisationId { get; set; }
    public string Name { get; set; } = "";
    public int MemberCount { get; set; }
    public List<int> PermissionIds { get; set; } = new();
    public List<RoleStaffDto> Members { get; set; } = new();
}

public class RoleStaffDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public bool IsActive { get; set; }
}

public record PermissionSettingOption(int Id, string Name);
public class PermissionSettingsData
{
    public bool IsSuperAdmin { get; set; }
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public List<PermissionSettingOption> Organisations { get; set; } = new();
    public List<PermissionDefinitionDto> Permissions { get; set; } = new();
    public List<AccessRoleDto> Roles { get; set; } = new();
    public List<PermissionSettingOption> Contexts { get; set; } = new();
    public List<PermissionSettingOption> Modules { get; set; } = new();
    public List<SiteMapContextDto> AreaMap { get; set; } = new();
}

public sealed class SiteMapContextDto
{
    public string ContextKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string BaseRoute { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<SiteMapMenuAreaDto> Areas { get; set; } = new();
}

public sealed class SiteMapMenuAreaDto
{
    public string AreaKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Route { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Description { get; set; } = "";
    public List<SiteMapSubFunctionDto> SubFunctions { get; set; } = new();
}

public sealed class SiteMapSubFunctionDto
{
    public string SubFunctionKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Route { get; set; } = "";
    public string Description { get; set; } = "";
    public List<SiteMapPermissionItemDto> Permissions { get; set; } = new();
}

public sealed class SiteMapPermissionItemDto
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ContextName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public bool IsSystem { get; set; }
}

public sealed record StaffAccessOption(int Id, string Name);
public sealed class StaffAccessPolicyDto
{
    public int Id { get; set; }
    public int? OrganisationId { get; set; }
    public int PositionClassificationId { get; set; }
    public string ClassificationName { get; set; } = "";
    public bool IsRestricted { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = "";
}
public sealed class StaffAccessScopeDto
{
    public int Id { get; set; }
    public int? OrganisationId { get; set; }
    public string SubjectType { get; set; } = "User";
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public bool RequireExplicitGrant { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class StaffAccessGrantDto
{
    public int Id { get; set; }
    public int? OrganisationId { get; set; }
    public string SubjectType { get; set; } = "User";
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string ResourceType { get; set; } = "Classification";
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = "";
    public bool IsAllowed { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAtUtc { get; set; }
    public string Reason { get; set; } = "";
}
public sealed class StaffAccessSettingsData
{
    public bool IsSuperAdmin { get; set; }
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public List<StaffAccessOption> Organisations { get; set; } = new();
    public List<StaffAccessOption> Users { get; set; } = new();
    public List<StaffAccessOption> Groups { get; set; } = new();
    public List<StaffAccessOption> Staff { get; set; } = new();
    public List<StaffAccessOption> Classifications { get; set; } = new();
    public List<StaffAccessOption> Units { get; set; } = new();
    public List<StaffAccessOption> Teams { get; set; } = new();
    public List<StaffAccessPolicyDto> Policies { get; set; } = new();
    public List<StaffAccessScopeDto> Scopes { get; set; } = new();
    public List<StaffAccessGrantDto> Grants { get; set; } = new();
    public List<OrganisationalAccessScopeDto> OrganisationalScopes { get; set; } = new();
    public List<UserAccessScopeAuditDto> ScopeAudits { get; set; } = new();
}

public sealed class OrganisationalAccessScopeDto
{
    public int Id { get; set; }
    public int? OrganisationId { get; set; }
    public string SubjectType { get; set; } = "User";
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string ScopeType { get; set; } = "DivisionAndDescendants";
    public int? UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public int? TeamId { get; set; }
    public string TeamName { get; set; } = "";
    public bool IncludeDescendants { get; set; } = true;
    public string PermissionFilter { get; set; } = "";
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string Reason { get; set; } = "";
    public string GrantedByName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UserAccessScopeAuditDto
{
    public long Id { get; set; }
    public int? ScopeId { get; set; }
    public string SubjectName { get; set; } = "";
    public string Action { get; set; } = "";
    public string ScopeType { get; set; } = "";
    public string TargetUnitOrTeam { get; set; } = "";
    public bool IncludeDescendants { get; set; }
    public string PerformedByName { get; set; } = "";
    public DateTime TimestampUtc { get; set; }
    public string Detail { get; set; } = "";
}

public sealed class EffectiveUnitAccessPreviewDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public string UnitType { get; set; } = "";
    public int? ParentUnitId { get; set; }
    public bool IsAllowed { get; set; }
    public string AccessLevel { get; set; } = "Restricted";
    public string AccessSummary { get; set; } = "";
    public int TotalStaffCount { get; set; }
    public int VisibleStaffCount { get; set; }
}

public sealed class EffectiveFieldAccessPreviewDto
{
    public string FieldCategory { get; set; } = "";
    public string PermissionCode { get; set; } = "";
    public bool IsAllowed { get; set; }
    public string StatusLabel { get; set; } = "Restricted";
}

public sealed class EffectiveAccessSimulationDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string HomeUnitName { get; set; } = "";
    public string OrganisationName { get; set; } = "";
    public string EffectiveScopeSummary { get; set; } = "";
    public bool HasFullOrganisationScope { get; set; }
    public int TotalOrganisationStaff { get; set; }
    public int TotalVisibleStaff { get; set; }
    public List<OrganisationalAccessScopeDto> ActiveScopes { get; set; } = new();
    public List<EffectiveUnitAccessPreviewDto> UnitAccessMatrix { get; set; } = new();
    public List<EffectiveFieldAccessPreviewDto> FieldAccessMatrix { get; set; } = new();
    public List<string> EffectivePermissions { get; set; } = new();
}
