using HRM.Services.Leave;
using HRM.Services.Leave.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class LeaveCarryForwardServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts, HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations
{
public Task<List<CarryForwardReview>> PendingAsync() => api.InvokeAsync<List<CarryForwardReview>>(typeof(HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations),nameof(PendingAsync), [],default);
public Task ProcessAsync(CarryForwardReview preview, decimal days, string reason) => api.InvokeAsync(typeof(HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations),nameof(ProcessAsync), [HRM.Application.Transport.ApiArgument.From(preview),HRM.Application.Transport.ApiArgument.From(days),HRM.Application.Transport.ApiArgument.From(reason)],default);
public Task<List<global::HRM.DTOs.Contracts.LeaveCarryForwardDto>> HistoryAsync() => api.InvokeAsync<List<global::HRM.DTOs.Contracts.LeaveCarryForwardDto>>(typeof(HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations),nameof(HistoryAsync), [],default);
}
