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

    public static int EarlyDepartureMinutes(DateTime actual, DateTime scheduled, int graceMinutes = 0)
    {
        var actualMinute = actual.Ticks / TimeSpan.TicksPerMinute;
        var allowedMinute = scheduled.AddMinutes(-Math.Max(0, graceMinutes)).Ticks / TimeSpan.TicksPerMinute;
        return (int)Math.Max(0, allowedMinute - actualMinute);
    }

    public static int CalculateLateMinutes(DateTime scheduled, DateTime actual, int graceMinutes = 0)
        => LateMinutes(actual, scheduled, graceMinutes);

    public static int CalculateEarlyDepartureMinutes(DateTime scheduled, DateTime actual, int graceMinutes = 0)
        => EarlyDepartureMinutes(actual, scheduled, graceMinutes);

    // Selects the clock event nearest the expected boundary. With no scheduled
    // boundary, the earliest event remains the fallback.
    public static T? Closest<T>(IEnumerable<T> items, Func<T, DateTime> timestamp, DateTime? expected)
        where T : class
    {
        var list = items.ToList();
        if (list.Count == 0) return null;
        if (!expected.HasValue) return list.OrderBy(timestamp).First();
        return list.OrderBy(x => Math.Abs((timestamp(x) - expected.Value).TotalMinutes))
            .ThenBy(timestamp)
            .First();
    }
}
