namespace HRM.Models;

public sealed class TeamScheduleAdjustment
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public DateTime WorkDate { get; set; }
    public TimeSpan? RevisedCheckIn { get; set; }
    public TimeSpan? RevisedCheckOut { get; set; }
    public string Reason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
    public Team Team { get; set; } = null!;
}
