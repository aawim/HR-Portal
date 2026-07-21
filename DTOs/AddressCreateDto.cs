namespace HRM.DTOs
{
    public class AddressCreateDto
    {
        public int CountryId { get; set; }

        public int? IslandId { get; set; }

        public int AddressBaseTypeId { get; set; }

        public int? AddressInstanceTypeId { get; set; }

        public int BusinessEntityLocationTypeId { get; set; }

        public string? Name { get; set; }

        public string? Floor { get; set; }

        public bool IsCurrent { get; set; }
    }
}
