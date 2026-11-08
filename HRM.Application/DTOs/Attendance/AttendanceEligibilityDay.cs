namespace HRM.Helpers;
public sealed record AttendanceEligibilityDay(bool HasApprovedPosition, bool HasActiveSchedule, bool IsScheduledDay, bool RequiresAttendance)
{
    public const string NoScheduleMessage = "No active schedule found";
    public bool CanBeAbsent => HasApprovedPosition && HasActiveSchedule && IsScheduledDay && RequiresAttendance;
    public string? Message => !HasActiveSchedule ? NoScheduleMessage : !HasApprovedPosition ? "Attendance starts from job position approval" : null;
}

