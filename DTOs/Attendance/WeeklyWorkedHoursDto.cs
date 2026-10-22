namespace HRM.DTOs.Attendance
{
    public class WeeklyWorkedHoursDto
    {
        public decimal CurrentWeekHours { get; set; }

        public decimal PreviousWeekHours { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public decimal ApprovedEarlyCheckoutShortfallHours { get; set; }
        public decimal MakeupCompletedHours { get; set; }
        public decimal MakeupRemainingHours { get; set; }

        public decimal DifferenceHours =>
            CurrentWeekHours - PreviousWeekHours;
    }
}
