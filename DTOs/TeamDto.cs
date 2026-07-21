namespace HRM.DTOs
{
    public class TeamDto
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameDhivehi { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IsValid { get; set; }
        public bool IsSuperVisor { get; set; }

        public List<TeamStaffDto> Staffs { get; set; } = new();
    }
}
