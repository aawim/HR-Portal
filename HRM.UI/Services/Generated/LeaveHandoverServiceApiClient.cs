using HRM.Services.Leave;
using HRM.Services.Leave.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class LeaveHandoverServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts, HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations
{
public Task<List<LeaveHandoverInboxItem>> InboxAsync(int? leaveId = null) => api.InvokeAsync<List<LeaveHandoverInboxItem>>(typeof(HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations),nameof(InboxAsync), [HRM.Application.Transport.ApiArgument.From(leaveId)],default);
public Task<List<LeaveHandoverInboxItem>> GetAssignedInboxAsync() => api.InvokeAsync<List<LeaveHandoverInboxItem>>(typeof(HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations),nameof(GetAssignedInboxAsync), [],default);
public Task<ServiceResult> DecideAsync(int leaveId, int revision, int? taskId, bool approve, string? comments) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations),nameof(DecideAsync), [HRM.Application.Transport.ApiArgument.From(leaveId),HRM.Application.Transport.ApiArgument.From(revision),HRM.Application.Transport.ApiArgument.From(taskId),HRM.Application.Transport.ApiArgument.From(approve),HRM.Application.Transport.ApiArgument.From(comments)],default);
public Task<ServiceResult> ReviseAsync(int leaveId, int revision, List<LeaveHandoverTaskInput> tasks) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations),nameof(ReviseAsync), [HRM.Application.Transport.ApiArgument.From(leaveId),HRM.Application.Transport.ApiArgument.From(revision),HRM.Application.Transport.ApiArgument.From(tasks)],default);
}
