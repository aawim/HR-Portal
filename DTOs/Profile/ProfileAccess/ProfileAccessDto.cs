namespace HRM.DTOs.Profile.ProfileAccess
{
    public class ProfileAccessDto
    {
        public List<ProfileTeamDto> Teams { get; set; } = [];

        public List<ProfileUserGroupDto> Groups { get; set; } = [];

        public List<ProfileRoleDto> Roles { get; set; } = [];

        public List<ProfilePermissionDto> Permissions { get; set; } = [];

        public List<ProfileSupervisingGroupDto> SupervisingGroups { get; set; } = [];
    }
}
