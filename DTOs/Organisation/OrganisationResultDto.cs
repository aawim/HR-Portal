using static HRM.Components.Admin.Hr.Organisation.OrganisationSearchGrid;

namespace HRM.DTOs.Organisation
{
    public class OrganisationResultDto
    {
        public int BusinessEntityID { get; set; }
        public string OrganisationName { get; set; } = string.Empty;
        public string OrganisationNameDhivehi { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public DateTime? RegistrationDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public string RegisteredAddress { get; set; } = string.Empty;
        public string MailingAddress { get; set; } = string.Empty;
        public List<TeamDto> Teams { get; set; } = new();

        public List<GroupDto> Groups { get; set; } = new();
    }
}
