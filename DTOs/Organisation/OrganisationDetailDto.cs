using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.Organisation
{
    public class OrganisationDetailDto
    {
        [Required(ErrorMessage = "Organisation Type is required")]
        public int? OrganisationTypeID { get; set; }

        [Required(ErrorMessage = "Registration Number is required")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Organisation Name is required")]
        public string OrganisationName { get; set; } = string.Empty;

        public string OrganisationNameDhivehi { get; set; } = string.Empty;

        public DateTime? RegistrationDate { get; set; }

        [Required(ErrorMessage = "Country is required")]
        public int? CountryID { get; set; }

        public int? ParentOrganisationBusinessEntityID { get; set; }
    }

}
