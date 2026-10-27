using HRM.Services.Leave;
using HRM.Services.Leave.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class LeaveAccrualSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts, HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations
{
public Task<global::HRM.DTOs.Contracts.LeaveAccrualSettingsDataResponse> LoadAsync() => api.InvokeAsync<global::HRM.DTOs.Contracts.LeaveAccrualSettingsDataResponse>(typeof(HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task SaveAsync(global::HRM.DTOs.Contracts.JobTypeLeaveAccrualRuleDto model) => api.InvokeAsync(typeof(HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<List<AccrualPreview>> PreviewAsync(int ruleId) => api.InvokeAsync<List<AccrualPreview>>(typeof(HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations),nameof(PreviewAsync), [HRM.Application.Transport.ApiArgument.From(ruleId)],default);
public Task<int> RunAsync(int ruleId) => api.InvokeAsync<int>(typeof(HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations),nameof(RunAsync), [HRM.Application.Transport.ApiArgument.From(ruleId)],default);
}
