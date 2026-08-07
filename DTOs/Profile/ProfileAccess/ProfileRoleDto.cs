namespace HRM.DTOs.Profile.ProfileAccess
{
    public class ProfileRoleDto
    {
        public int RoleId { get; set; }

        public string RoleKey { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public int? UserGroupId { get; set; }

        public string? UserGroupName { get; set; }

        public int? OrganisationId { get; set; }

        public bool IsSystemRole { get; set; }

        public bool IsActive { get; set; }
    }
}
