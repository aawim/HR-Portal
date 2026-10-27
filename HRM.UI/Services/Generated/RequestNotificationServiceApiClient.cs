using HRM.Services;
using HRM.Services.Contracts;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Components.Shared;
using HRM.Models.WorkPlanning;
namespace HRM.UI.Services.Generated;
public sealed class RequestNotificationServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.RequestNotificationServiceContracts, HRM.Services.Contracts.RequestNotificationServiceContracts.IOperations
{
public Task<RequestNotificationInbox> GetInboxAsync(CancellationToken token = default) => api.InvokeAsync<RequestNotificationInbox>(typeof(HRM.Services.Contracts.RequestNotificationServiceContracts.IOperations),nameof(GetInboxAsync), [],token);
public Task MarkReadAsync(long? notificationId = null, CancellationToken token = default) => api.InvokeAsync(typeof(HRM.Services.Contracts.RequestNotificationServiceContracts.IOperations),nameof(MarkReadAsync), [HRM.Application.Transport.ApiArgument.From(notificationId)],token);
}
