namespace HRM.DTOs.Attendance
{
    public class WeeklyWorkedHoursDto
    {
        public decimal CurrentWeekHours { get; set; }

        public decimal PreviousWeekHours { get; set; }

        public decimal DifferenceHours =>
            CurrentWeekHours - PreviousWeekHours;
    }
}
