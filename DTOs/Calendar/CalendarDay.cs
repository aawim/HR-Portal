namespace HRM.DTOs.Calendar
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }

        public bool IsCurrentMonth { get; set; }

        public bool IsToday { get; set; }

        public List<CalendarEvent> Events { get; set; } = new();
    }
}
