namespace HRM.DTOs.Roles;

public class PermissionDefinitionDto
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ContextId { get; set; } = 1;
    public int? ModuleId { get; set; }
    public bool IsSystem { get; set; }
    public string Group => PermissionArea(Key);
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
