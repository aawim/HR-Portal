using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using System.Text.Json;
using HRM.Models;
using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Components.Shared;
using HRM.Models.Workflow;
namespace HRM.UI.Services.Generated;
public sealed class OvertimeRequestServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.OvertimeRequestServiceContracts, HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations
{
public Task<ServiceResult> SubmitAsync(DateTime date, int minutes, string reason) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(SubmitAsync), [HRM.Application.Transport.ApiArgument.From(date),HRM.Application.Transport.ApiArgument.From(minutes),HRM.Application.Transport.ApiArgument.From(reason)],default);
public Task<List<OvertimeRow>> LoadAsync(bool pendingOnly=false) => api.InvokeAsync<List<OvertimeRow>>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(pendingOnly)],default);
public Task<ServiceResult> ActAsync(int id,int expectedState,string action,string? comment) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(ActAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
public Task<ServiceResult> RecordPaymentAsync(int id,string reference) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(RecordPaymentAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(reference)],default);
public Task<ServiceResult> ProcessPayrollAsync(IEnumerable<int> requestIds,DateTime month,string reference,Guid clientBatchId) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(ProcessPayrollAsync), [HRM.Application.Transport.ApiArgument.From(requestIds),HRM.Application.Transport.ApiArgument.From(month),HRM.Application.Transport.ApiArgument.From(reference),HRM.Application.Transport.ApiArgument.From(clientBatchId)],default);
public Task<ServiceResult> ResubmitClaimAsync(int id,string reason) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(ResubmitClaimAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(reason)],default);
public Task<List<OvertimeClockOption>> LoadClockEventsAsync(DateTime date) => api.InvokeAsync<List<OvertimeClockOption>>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(LoadClockEventsAsync), [HRM.Application.Transport.ApiArgument.From(date)],default);
public Task<List<OvertimeTaskOption>> LoadApprovedTasksAsync() => api.InvokeAsync<List<OvertimeTaskOption>>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(LoadApprovedTasksAsync), [],default);
public Task<ServiceResult> SubmitClaimAsync(int startLogId, int endLogId, string reason, int? taskId = null) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(SubmitClaimAsync), [HRM.Application.Transport.ApiArgument.From(startLogId),HRM.Application.Transport.ApiArgument.From(endLogId),HRM.Application.Transport.ApiArgument.From(reason),HRM.Application.Transport.ApiArgument.From(taskId)],default);
public Task<ServiceResult> SubmitWorkAsync(string kind, DateTime from, DateTime to, string reason, List<string> tasks,
        DateTime? proposedStart = null, DateTime? proposedEnd = null) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(SubmitWorkAsync), [HRM.Application.Transport.ApiArgument.From(kind),HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to),HRM.Application.Transport.ApiArgument.From(reason),HRM.Application.Transport.ApiArgument.From(tasks),HRM.Application.Transport.ApiArgument.From(proposedStart),HRM.Application.Transport.ApiArgument.From(proposedEnd)],default);
public Task<List<OvertimeWorkRow>> LoadWorkAsync(bool pendingOnly = false) => api.InvokeAsync<List<OvertimeWorkRow>>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(LoadWorkAsync), [HRM.Application.Transport.ApiArgument.From(pendingOnly)],default);
public Task<ServiceResult> ActWorkAsync(int id, int expectedState, string action, string? comment) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),nameof(ActWorkAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
}
