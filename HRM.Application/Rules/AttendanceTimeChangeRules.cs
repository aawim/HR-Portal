namespace HRM.Helpers;

public static class AttendanceTimeChangeRules
{
    public static HRM.DTOs.Attendance.StaffScheduleAttendanceDto ResolveSchedule(
        IReadOnlyList<HRM.DTOs.Attendance.StaffScheduleAttendanceDto> schedules, TimeOnly? time, string kind)
    {
        var official = schedules.Where(s => s.Purpose.Equals("Official", StringComparison.OrdinalIgnoreCase)).ToList();
        var available = official.Count > 0 ? official : schedules.ToList();
        if (available.Count == 0) throw new InvalidOperationException("No eligible working hours were found for this date. Please contact HR if your schedule is missing.");
        if (available.Count == 1) return available[0];
        if (!time.HasValue) throw new InvalidOperationException("Choose a time to identify the applicable working hours.");
        var matches = available.Where(s =>
        {
            var requested = RequestedDateTime(s, time.Value);
            return requested > s.ScheduledStart && requested < s.ScheduledEnd;
        }).ToList();
        if (matches.Count == 1) return matches[0];
        throw new InvalidOperationException(matches.Count == 0 ? "The selected time is outside your working hours for this date." : "More than one set of working hours matches this time. Please contact HR to review the overlap.");
    }
    public static DateTime RequestedDateTime(HRM.DTOs.Attendance.StaffScheduleAttendanceDto schedule, TimeOnly time)
    {
        var day = schedule.ScheduledStart.Date;
        var value = day.Add(time.ToTimeSpan());
        return schedule.ScheduledEnd.Date > day && value <= schedule.ScheduledStart ? value.AddDays(1) : value;
    }
    public static (DateTime Start, DateTime End) Calculate(DateTime start, DateTime end, DateTime requested, string kind)
    {
        if (end <= start || end - start > TimeSpan.FromHours(24)) throw new InvalidOperationException("This schedule has an invalid working duration. Contact HR.");
        if (kind == "LateCheckIn")
        {
            if (requested <= start || requested >= end) throw new InvalidOperationException("Choose a check-in time after the scheduled start and before the scheduled finish.");
            return (requested, requested.Add(end - start));
        }
        if (kind == "EarlyCheckOut")
        {
            if (requested <= start || requested >= end) throw new InvalidOperationException("Choose a check-out time after the scheduled start and before the scheduled finish.");
            return (start, requested);
        }
        throw new InvalidOperationException("Select a valid attendance time-change request.");
    }
}
