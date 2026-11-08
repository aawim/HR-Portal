
namespace HRM.Models;

public sealed class LateCheckInRequest
{
    public int RequestId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AttendanceLogId { get; set; }
    public string Kind { get; set; } = "LateCheckIn";
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RequestedStart { get; set; }
    public DateTime? RequestedEnd { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
    public long WorkAssignmentId { get; set; }
    public string ScheduleName { get; set; } = "";
    public DateTime ScheduledStart { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public int LateMinutes { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
}
