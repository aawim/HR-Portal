using HRM.Components.Shared;
namespace HRM.Services.Contracts;
public abstract class RequestNotificationServiceContracts
{

public interface IOperations
{
Task<RequestNotificationInbox> GetInboxAsync(CancellationToken token = default)
;
Task MarkReadAsync(long? notificationId = null, CancellationToken token = default)
;
}
}
