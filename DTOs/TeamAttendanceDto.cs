namespace HRM.DTOs
{
    public class TeamAttendanceDto
    {
        public int StaffID { get; set; }

        public int TeamID { get; set; }

        public string TeamName { get; set; } = "";

        public string Name { get; set; } = "";

        public string NameDhivehi { get; set; } = "";

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        public AttendanceStatus Status { get; set; }
    }

    public enum AttendanceStatus
    {
        Present,
        Late,
        MissingCheckout,
        Absent,
        FamilyLeave,
        SickLeave
    }
}
