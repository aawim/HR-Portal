namespace HRM.DTOs.Attendance;

public sealed record StaffAttendanceClockEventDto(
    DateTime Time,
    string Event,
    string Schedule,
    bool IsSharedAcrossSchedules = false,
    string? SharedScheduleSummary = null,
    string? LogicalEventType = null,
    string? MatchMethod = null,
    string? MatchReason = null,
    string? ConfidenceState = null,
    int? AttendanceLogId = null)
{
    public bool IsPrimaryClassification { get; init; } = true;
    public bool IsSharedEvent => IsSharedAcrossSchedules;
}
