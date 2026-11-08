using HRM.DTOs.Roles;

namespace HRM.DTOs;

public enum UserSettingsTab { Accounts, Permissions, Staff, Enrollment, Organisation }
public class ManagedUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Mobile { get; set; }
    public bool Active { get; set; }
    public bool Approved { get; set; }
    public int StateId { get; set; }
    public string SsoStatus { get; set; } = "";
    public List<int> DirectPermissionIds { get; set; } = new();
    public List<int> RoleIds { get; set; } = new();
}
public class ManagedStaffDto
{
    public int IndividualId { get; set; }
    public string Name { get; set; } = "";
    public string? EmployeeNumber { get; set; }
}
public class ManagedEnrollmentDto
{
    public int Id { get; set; }
    public int IndividualId { get; set; }
    public string Name { get; set; } = "";
    public string Number { get; set; } = "";
}
public class UserSettingsData
{
    public string OrganisationName { get; set; } = "";
    public int UserOrganisationId { get; set; }
    public List<ManagedUserDto> Users { get; set; } = new();
    public List<ManagedStaffDto> Staff { get; set; } = new();
    public List<ManagedStaffDto> People { get; set; } = new();
    public List<ManagedEnrollmentDto> Enrollments { get; set; } = new();
    public List<PermissionDefinitionDto> Permissions { get; set; } = new();
    public List<AccessRoleDto> Roles { get; set; } = new();
    public List<int> OrganisationPermissionIds { get; set; } = new();
    public List<PermissionSettingOption> AccountStates { get; set; } = new();
}
