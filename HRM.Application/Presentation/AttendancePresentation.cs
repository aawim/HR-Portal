using HRM.DTOs.Attendance;

namespace HRM.Helpers;

public static class AttendancePresentation
{
    public static string Attended(bool late, int minutes, bool complete) => late
        ? $"Attended late by {Math.Max(1, minutes)} {(Math.Max(1, minutes) == 1 ? "minute" : "minutes") }"
        : complete ? "Perfectly attended" : "Attended";

    public static string Status(YearlyAttendanceDayDto day) => day.IsOnLeave ? $"Leave · {day.LeaveTypeName ?? "Leave"}" :
        day.IsPresent ? Attended(day.IsLate, day.LateMinutes, day.IsPerfectlyAttended) :
        day.IsRestDay ? "Rest day" : day.IsHoliday ? "Holiday" : day.IsUnresolved ? "Unresolved" :
        day.IsLate ? $"Check in overdue by {day.LateMinutes} minutes" : day.IsAbsent ? "Absent" : day.EligibilityMessage ?? "—";
}
