using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using HRM.Components.Shared;
namespace HRM.UI.Services.Generated;
public sealed class LeaveConfigurationSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts, HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations
{
public Task<global::HRM.DTOs.Contracts.LeaveConfigurationDataResponse> LoadAsync() => api.InvokeAsync<global::HRM.DTOs.Contracts.LeaveConfigurationDataResponse>(typeof(HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task SaveAsync(global::HRM.DTOs.Contracts.LeaveConfigurableValueDto model) => api.InvokeAsync(typeof(HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task DeleteAsync(int id) => api.InvokeAsync(typeof(HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations),nameof(DeleteAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
}
