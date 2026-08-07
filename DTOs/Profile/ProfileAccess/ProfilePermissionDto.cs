namespace HRM.DTOs.Profile.ProfileAccess
{
    public class ProfilePermissionDto
    {
        public int PermissionId { get; set; }

        public string PermissionName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ModuleName { get; set; }

        public string Source { get; set; } = string.Empty;

        public int? RoleId { get; set; }

        public string? RoleName { get; set; }

        public int? UserGroupId { get; set; }

        public string? UserGroupName { get; set; }

        public int? OrganisationId { get; set; }

        public bool IsGlobal { get; set; }
    }
}
