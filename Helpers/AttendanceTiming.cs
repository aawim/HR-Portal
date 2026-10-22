namespace HRM.Helpers;

public static class AttendanceTiming
{
    // Attendance is presented and assessed to the minute. Preserve seconds in raw logs.
    public static int LateMinutes(DateTime actual, DateTime scheduled, int graceMinutes = 0)
    {
        var actualMinute = actual.Ticks / TimeSpan.TicksPerMinute;
        var allowedMinute = scheduled.AddMinutes(Math.Max(0, graceMinutes)).Ticks / TimeSpan.TicksPerMinute;
        return (int)Math.Max(0, actualMinute - allowedMinute);
    }
}
