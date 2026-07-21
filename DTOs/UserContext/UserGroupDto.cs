namespace HRM.DTOs.UserContext
{
    public class UserGroupDto
    {
        public int UserGroupID { get; set; }

        public string GroupName { get; set; } = string.Empty;

        public int? UserOrganisationID { get; set; }

        public bool IsGlobal { get; set; }

        public int? ParentUserGroupID { get; set; }
    }
}
