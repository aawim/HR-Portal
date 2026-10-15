using HRM.DTOs.Leave;

namespace HRM.Helpers;

public static class LeaveChangeRules
{
    public static (DateTime EndDate, int Duration, int BalanceAdjustment) Calculate(
        DateTime from, DateTime currentEnd, int currentDuration,
        LeaveChangeKind kind, DateTime requestedDate, bool includeHolidays)
    {
        if (requestedDate == default || !System.Enum.IsDefined(kind))
            throw new InvalidOperationException("Select a valid change type and date.");
        var end = kind == LeaveChangeKind.EarlyReturn ? requestedDate.Date.AddDays(-1) : requestedDate.Date;
        if (kind == LeaveChangeKind.Extension && end <= currentEnd.Date)
            throw new InvalidOperationException("The extended end date must be after the current end date.");
        if (kind == LeaveChangeKind.EarlyReturn && (end < from.Date || end >= currentEnd.Date))
            throw new InvalidOperationException("The return-to-work date must be after the leave starts and on or before its current end date.");
        var duration = LeaveApplicationDates.Count(from, end, includeHolidays);
        if (duration <= 0) throw new InvalidOperationException("The changed leave must include at least one leave day.");
        return (end, duration, currentDuration - duration);
    }
}
