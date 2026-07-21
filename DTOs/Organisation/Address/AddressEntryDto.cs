namespace HRM.DTOs.Organisation.Address
{
    public class AddressEntryDto
    {
        public int? RegisteredAddressId { get; set; }
        public bool CreateNewRegisteredAddress { get; set; } = false;

        public int? NewAddressInstanceTypeID { get; set; }
        public int? NewLocationTypeID { get; set; }

        public string NewAddressLine1 { get; set; } = string.Empty;
        public string NewAddressLine2 { get; set; } = string.Empty;
        public string NewAddressName { get; set; } = string.Empty;
        public string NewAddressFloor { get; set; } = string.Empty;
        public string NewRoad { get; set; } = string.Empty;

        public int? NewCountryID { get; set; }
        public int? NewAtollID { get; set; }
        public int? NewIslandID { get; set; }
        public int? NewWardID { get; set; }

        public string NewPostCode { get; set; } = string.Empty;
        public string NewMunicipalityNumber { get; set; } = string.Empty;
    }
}
