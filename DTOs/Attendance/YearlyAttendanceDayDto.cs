namespace HRM.DTOs.Attendance
{
    public class YearlyAttendanceDayDto
    {
        public DateTime Date { get; set; }

        public bool HasAttendanceLogs { get; set; }

        public bool IsPresent { get; set; }

        public bool IsAbsent { get; set; }

        public bool IsUnresolved { get; set; }

        public bool IsRestDay { get; set; }

        public bool IsOnLeave { get; set; }

        public bool IsHoliday { get; set; }

        public int ClockEventCount { get; set; }

        public int ResolvedEventCount { get; set; }

        public int UnresolvedEventCount { get; set; }

        public DateTime? FirstCheckIn { get; set; }

        public DateTime? LastCheckOut { get; set; }

        public decimal WorkedHours { get; set; }
    }
}
