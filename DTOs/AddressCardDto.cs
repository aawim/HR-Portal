namespace HRM.DTOs
{
    public class AddressCardDto
    {
        public int LocationId { get; set; }
        public int CountryId { get; set; }
        public int? AtollId { get; set; }
        public int? IslandId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public List<HRM.DTOs.Profile.ProfileContactDto> Contacts { get; set; } = new();

        public string Title { get; set; } = "";

        public string Country { get; set; } = "";

        public string Atoll { get; set; } = "";

        public string Island { get; set; } = "";

        public string Address { get; set; } = "";

        public string? Floor { get; set; }

        public string LocationType { get; set; } = "";

        public bool IsCurrent { get; set; }

 

    }
}
