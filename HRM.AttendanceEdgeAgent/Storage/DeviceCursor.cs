namespace HRM.AttendanceEdgeAgent.Storage;

public sealed class DeviceCursor
{
    public int DeviceId { get; set; }
    public string? LastEventId { get; set; }
    public DateTime? LastEventTimestampUtc { get; set; }
    public DateTime LastPollUtc { get; set; } = DateTime.UtcNow;
    public long TotalEventsIngested { get; set; }
}
