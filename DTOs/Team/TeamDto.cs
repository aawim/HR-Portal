
namespace HRM.DTOs.Team
{
    public class TeamDto
    {
        public int TeamID { get; set; }

        public string? Name { get; set; }

        public string? NameDhivehi { get; set; }

        public List<TeamStaffDto> Staffs { get; set; } = new();
    }
}
