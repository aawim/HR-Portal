namespace HRM.DTOs.UserContext
{
    public class UserOrganisationAccessDto
    {
        public int UserOrganisationID { get; set; }


        public int BusinessEntityID { get; set; }


        public string OrganisationName { get; set; } = string.Empty;


        public List<string> UserGroups { get; set; } = new();


        public List<string> Permissions { get; set; } = new();
    }
}
