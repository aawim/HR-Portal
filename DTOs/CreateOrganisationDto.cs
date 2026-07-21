using System.ComponentModel.DataAnnotations;
namespace HRM.DTOs
{
    public class CreateOrganisationDto
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

        public int? CSCOfficePimaryKey { get; set; }

        // --- TAB 2: ADDRESSES ---
        public int? RegisteredAddressId { get; set; }
        public bool CreateNewRegisteredAddress { get; set; } = false;

        // Base & Location Types
        public int? NewAddressTypeID { get; set; }
        public int? NewLocationTypeID { get; set; }

        // Instance Details (Specific office/suite)
        public string NewAddressName { get; set; } = string.Empty;
        public string NewAddressFloor { get; set; } = string.Empty;

        public int? NewAddressInstanceTypeID { get; set; }

 

        // Physical Street Address
        public string NewAddressLine1 { get; set; } = string.Empty;
        public string NewAddressLine2 { get; set; } = string.Empty;
        public string NewRoad { get; set; } = string.Empty;
        public string NewPostCode { get; set; } = string.Empty;
        public string NewMunicipalityNumber { get; set; } = string.Empty;

        // Geographic Linking
        public int? NewCountryID { get; set; }
        public int? NewAtollID { get; set; }
        public int? NewIslandID { get; set; }
        public int? NewWardID { get; set; }






        // --- TAB 3: Contacts ---

        //public List<ContactField> DynamicContacts { get; set; } = new();
        public List<ContactField> DynamicContacts { get; set; } = new();

        //public string? Mobile { get; set; } = string.Empty;
        //public string? Phone { get; set; } = string.Empty;
        //public string? Fax { get; set; } = string.Empty;



        private string? _email;

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email
        {
            get => _email;
            // Intercept empty strings from Blazor and force them to null
            set => _email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
 
        public string? Website { get; set; } = string.Empty;
    }


}
