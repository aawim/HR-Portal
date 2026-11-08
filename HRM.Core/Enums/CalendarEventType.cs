namespace HRM.Components.Shared.Calendar
{
    public enum CalendarEventType
    {
        Leave,
        Birthday,
        Holiday,
        Event,
        Meeting,
        Training,
        Overtime,
        Attendance,
        Reminder,
        Announcement
    }


    //private string GetColor(CalendarEventType type)
    //{
    //    return type switch
    //    {
    //        CalendarEventType.Leave => "bg-blue-500",
    //        CalendarEventType.Birthday => "bg-pink-500",
    //        CalendarEventType.Holiday => "bg-red-500",
    //        CalendarEventType.Meeting => "bg-purple-500",
    //        CalendarEventType.Training => "bg-indigo-500",
    //        CalendarEventType.Event => "bg-green-500",
    //        CalendarEventType.Overtime => "bg-amber-500",
    //        CalendarEventType.Attendance => "bg-cyan-500",
    //        CalendarEventType.Reminder => "bg-orange-500",
    //        _ => "bg-slate-500"
    //    };
    //}

}
