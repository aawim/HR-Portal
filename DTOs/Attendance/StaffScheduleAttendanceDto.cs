namespace HRM.DTOs.Attendance;

public sealed record StaffScheduleAttendanceDto(DateTime Date, string Name, string Purpose, bool IsShiftOrDuty,
    bool RequiresAttendance, DateTime ScheduledStart, DateTime ScheduledEnd, DateTime? CheckIn, DateTime? CheckOut);
