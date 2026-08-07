namespace HRM.DTOs.Profile.ProfileAccess
{
    public class ProfileUserGroupDto
    {
        public int UserGroupId { get; set; }

        public string GroupName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? OrganisationId { get; set; }

        public bool IsActive { get; set; }
    }
}
