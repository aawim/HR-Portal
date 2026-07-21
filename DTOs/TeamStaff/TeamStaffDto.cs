namespace HRM.DTOs.TeamStaff
{
    public class TeamStaffDto
    {
        public int StaffID { get; set; }

        public bool IsSuperVisor { get; set; }

        public string Name { get; set; } = "";

        public string NameDhivehi { get; set; } = "";
    }
}
