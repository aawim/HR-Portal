using HRM.Models;
using HRM.Models.Workflow;
namespace HRM.Services;

public sealed record RequestNotificationItem(long Id, string Reference, string FromState, string ToState,
    DateTime CreatedAtUtc, bool IsRead, string Url, string? ActionStage = null, string? Message = null);

public sealed record RequestNotificationInbox(int UnreadCount, List<RequestNotificationItem> Items);
