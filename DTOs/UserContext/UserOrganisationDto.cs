namespace HRM.DTOs.UserContext
{
    public class UserOrganisationDto
    {
        public int OrganisationBusinessEntityId { get; set; }

        public string OrganisationName { get; set; } = string.Empty;

        public string OrganisationNameDhivehi { get; set; } = string.Empty;


        public int OrganisationStructureId { get; set; }

        public string OrganisationStructureName { get; set; } = string.Empty;


        public int? ParentOrganisationBusinessEntityId { get; set; }
    }
}
