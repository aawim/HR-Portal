namespace HRM.DTOs.Profile
{
    public class ProfileContactDto
    {
        public int ContactId { get; set; }

        public int BusinessEntityId { get; set; }

        public int ContactInformationTypeId { get; set; }

        public string ContactTypeName { get; set; } =
            string.Empty;

        public string Value { get; set; } =
            string.Empty;

        public bool IsValid { get; set; }

    }
}
