namespace HRM.Components.Shared.Calendar
{
    public class CalendarEvent
    {
        public int Id { get; set; }

        public DateOnly Date { get; set; }

        public string Title { get; set; } = "";

        public CalendarEventType Type { get; set; }

        public string? Description { get; set; }

        public string? Url { get; set; }

        public object? Data { get; set; }
    }
}
