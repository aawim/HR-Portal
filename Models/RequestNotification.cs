namespace HRM.Models;

public sealed class RequestNotification
{
    public long RequestNotificationId { get; set; }
    public int UserId { get; set; }
    public int RequestId { get; set; }
    public int FromStateId { get; set; }
    public int ToStateId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public Request Request { get; set; } = null!;
}
