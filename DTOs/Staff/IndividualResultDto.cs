namespace HRM.DTOs.Staff
{
    public class IndividualResultDto
    {
        public int BusinessEntityId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Organisation { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
    }
}
