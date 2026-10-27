using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class JobTypeOvertimeSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts, HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations
{
public Task<global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto> LoadAsync(int jobTypeId) => api.InvokeAsync<global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto>(typeof(HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(jobTypeId)],default);
public Task SaveAsync(global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto model) => api.InvokeAsync(typeof(HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
}
