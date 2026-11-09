namespace HRM.DTOs.Roles;

public class PermissionDefinitionDto
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ContextId { get; set; } = 1;
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
    public List<PermissionDefinitionDto> Permissions { get; set; } = new();
    public List<AccessRoleDto> Roles { get; set; } = new();
    public List<PermissionSettingOption> Contexts { get; set; } = new();
    public List<PermissionSettingOption> Modules { get; set; } = new();
}

public sealed record StaffAccessOption(int Id, string Name);
public sealed class StaffAccessPolicyDto
{
    public int Id { get; set; }
    public int PositionClassificationId { get; set; }
    public string ClassificationName { get; set; } = "";
    public bool IsRestricted { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = "";
}
public sealed class StaffAccessScopeDto
{
    public int Id { get; set; }
    public string SubjectType { get; set; } = "User";
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public bool RequireExplicitGrant { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class StaffAccessGrantDto
{
    public int Id { get; set; }
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
    public int OrganisationId { get; set; }
    public List<StaffAccessOption> Users { get; set; } = new();
    public List<StaffAccessOption> Groups { get; set; } = new();
    public List<StaffAccessOption> Staff { get; set; } = new();
    public List<StaffAccessOption> Classifications { get; set; } = new();
    public List<StaffAccessPolicyDto> Policies { get; set; } = new();
    public List<StaffAccessScopeDto> Scopes { get; set; } = new();
    public List<StaffAccessGrantDto> Grants { get; set; } = new();
}
