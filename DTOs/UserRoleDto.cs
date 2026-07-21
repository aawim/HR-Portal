namespace HRM.DTOs
{
    public class UserRoleDto
    {
        public int RoleId { get; set; }

        public string RoleKey { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;


        public int? UserOrganisationID { get; set; }


        public string Source { get; set; } = string.Empty;


        public bool IsSystemRole { get; set; }

        public bool IsActive { get; set; }
    }
}
