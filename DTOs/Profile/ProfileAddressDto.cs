namespace HRM.DTOs.Profile
{
    public class ProfileAddressDto
    {
        public int AddressId { get; set; }

        public int BusinessEntityId { get; set; }

        public int? AddressTypeId { get; set; }

        public string AddressTypeName { get; set; } =
            string.Empty;

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? IslandName { get; set; }

        public string? AtollName { get; set; }

        public string? CityName { get; set; }

        public string? CountryName { get; set; }

        public string? PostalCode { get; set; }

        public string? Description { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsValid { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
    }
}
