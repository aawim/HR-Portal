namespace HRM.DTOs.Organisation.Address
{
    public class AddressSearchResultDto
    {
        public int AddressBaseID { get; set; }
        public string AddressType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string HouseBuilding { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
    }
}
