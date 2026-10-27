
namespace HRM.Models;
public sealed class StaffNotification
{
    public long StaffNotificationId { get; set; }
    public int UserId { get; set; }
    public int IndividualId { get; set; }
    public int OrganisationId { get; set; }
    public string Kind { get; set; } = "";
    public string Message { get; set; } = "";
    public string Url { get; set; } = "/online";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
