namespace HRM.DTOs.Attendance
{
    public class WeeklyWorkedHoursDto
    {
        public bool HasWorkingHoursRule { get; set; }
        public string? ConfigurationMessage { get; set; }
        public decimal RequiredWeekHours { get; set; }
        public decimal RemainingWeekHours { get; set; }
        public decimal OfficialHours { get; set; }
        public decimal DutyHours { get; set; }
        public decimal DutyOverlapHours { get; set; }
        public decimal DutyOutsideOfficialHours { get; set; }
        public bool DutyCountsTowardWeek { get; set; }
        public decimal AmendmentCreditHours { get; set; }
        public decimal LeaveCreditHours { get; set; }
        public decimal PotentialOvertimeHours { get; set; }
        public decimal ApprovedExceptionHours { get; set; }
        public decimal ExceptionCreditHours { get; set; }
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
