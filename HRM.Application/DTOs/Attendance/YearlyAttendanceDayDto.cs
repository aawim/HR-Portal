namespace HRM.DTOs.Attendance
{
    public class YearlyAttendanceDayDto
    {
        public DateTime Date { get; set; }

        public bool HasAttendanceLogs { get; set; }

        public bool IsPresent { get; set; }
        public bool IsLate { get; set; }
        public int LateMinutes { get; set; }
        public bool IsPerfectlyAttended { get; set; }
        public DateTime? ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }

        public bool IsAbsent { get; set; }
        public bool HasActiveSchedule { get; set; }
        public bool HasApprovedPosition { get; set; }
        public bool RequiresAttendance { get; set; }
        public string? EligibilityMessage { get; set; }

        public bool IsUnresolved { get; set; }

        public bool IsRestDay { get; set; }

        public bool IsOnLeave { get; set; }
        public string? LeaveTypeName { get; set; }

        public bool IsHoliday { get; set; }

        public int ClockEventCount { get; set; }

        public int ResolvedEventCount { get; set; }

        public int UnresolvedEventCount { get; set; }

        public DateTime? FirstCheckIn { get; set; }

        public DateTime? LastCheckOut { get; set; }

        public decimal WorkedHours { get; set; }
    }
}
