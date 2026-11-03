namespace HRM.DTOs.Attendance;

public sealed record StaffScheduleAttendanceDto(DateTime Date, string Name, string Purpose, bool IsShiftOrDuty,
    bool RequiresAttendance, DateTime ScheduledStart, DateTime ScheduledEnd, DateTime? CheckIn, DateTime? CheckOut)
{
    public long WorkAssignmentId { get; init; }
    public int? CheckInLogId { get; init; }
    public int? CheckOutLogId { get; init; }
    public DateTime? BreakOut { get; init; }
    public DateTime? BreakIn { get; init; }
    public int PlannedMinutes { get; init; }
    public int ActualPresenceMinutes { get; init; }
    public int CreditedWorkedMinutes { get; init; }
    public int CreditedMinutes => CreditedWorkedMinutes;
    public int LateMinutes { get; init; }
    public int EarlyDepartureMinutes { get; init; }
    public string ObligationState { get; init; } = "NotStarted";
    public string? StatusReason { get; init; }
    public bool SharesCheckInEvent { get; init; }
    public bool SharesCheckOutEvent { get; init; }
    public bool SharesCheckInWithOfficial => SharesCheckInEvent;
    public bool SharesCheckOutWithOfficial => SharesCheckOutEvent;
}
