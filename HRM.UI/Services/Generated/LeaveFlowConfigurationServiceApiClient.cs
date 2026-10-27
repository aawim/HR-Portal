using HRM.Services.Leave;
using HRM.Services.Leave.Contracts;
using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class LeaveFlowConfigurationServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts, HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts.IOperations
{
public Task<LeaveFlowSettingsData> LoadAsync() => api.InvokeAsync<LeaveFlowSettingsData>(typeof(HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task<LeaveFlowConfigurationDto> GetAsync(int jobType, int requestType) => api.InvokeAsync<LeaveFlowConfigurationDto>(typeof(HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts.IOperations),nameof(GetAsync), [HRM.Application.Transport.ApiArgument.From(jobType),HRM.Application.Transport.ApiArgument.From(requestType)],default);
public Task<ServiceResult> SaveAsync(LeaveFlowConfigurationDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
}
