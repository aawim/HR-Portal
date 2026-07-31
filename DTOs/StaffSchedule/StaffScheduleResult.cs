namespace HRM.DTOs.StaffSchedule
{
    public sealed class StaffScheduleResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public StaffScheduleDto? Schedule { get; set; }

        public static StaffScheduleResult Ok(
            StaffScheduleDto schedule)
        {
            return new StaffScheduleResult
            {
                Success = true,
                Schedule = schedule
            };
        }

        public static StaffScheduleResult Failure(
            string message)
        {
            return new StaffScheduleResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
