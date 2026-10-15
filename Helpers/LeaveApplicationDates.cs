namespace HRM.Helpers
{
    public static class LeaveApplicationDates
    {
        public static bool IsCounted(DateTime date, bool includeHolidays) =>
            includeHolidays || (date.DayOfWeek != DayOfWeek.Friday && date.DayOfWeek != DayOfWeek.Saturday);

        public static int Count(DateTime from, DateTime to, bool includeHolidays)
        {
            if (to.Date < from.Date) return 0;
            if (includeHolidays) return (to.Date - from.Date).Days + 1;
            var days = 0;
            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
                if (IsCounted(date, false)) days++;
            return days;
        }

        public static DateTime EndDate(DateTime from, int duration, bool includeHolidays)
        {
            var date = from.Date;
            var remaining = Math.Max(1, duration);
            while (true)
            {
                if (IsCounted(date, includeHolidays) && --remaining == 0) return date;
                date = date.AddDays(1);
            }
        }
    }
}
