using HRM.Services.Leave;
using HRM.Services.Leave.Contracts;
using System.Data;
using System.Text.Json;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class LeaveHandoverRuleSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts, HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations
{
public Task<HandoverRuleSettingsData> LoadAsync() => api.InvokeAsync<HandoverRuleSettingsData>(typeof(HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task<List<HandoverOption>> StaffAsync(string search) => api.InvokeAsync<List<HandoverOption>>(typeof(HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations),nameof(StaffAsync), [HRM.Application.Transport.ApiArgument.From(search)],default);
public Task<EffectiveHandoverRule> PreviewAsync(int jobId,int typeId,DateTime date) => api.InvokeAsync<EffectiveHandoverRule>(typeof(HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations),nameof(PreviewAsync), [HRM.Application.Transport.ApiArgument.From(jobId),HRM.Application.Transport.ApiArgument.From(typeId),HRM.Application.Transport.ApiArgument.From(date)],default);
public Task SaveAsync(HandoverRuleEdit m) => api.InvokeAsync(typeof(HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(m)],default);
}
