using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class ScopeScheduleSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts, HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations
{
public Task<global::HRM.DTOs.Contracts.ScopeScheduleSettingsDataResponse> LoadAsync(int? organisationId=null) => api.InvokeAsync<global::HRM.DTOs.Contracts.ScopeScheduleSettingsDataResponse>(typeof(HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task SaveAsync(global::HRM.DTOs.Contracts.ScopeScheduleAssignmentDto model,int? organisationId=null) => api.InvokeAsync(typeof(HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(model),HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task SetEnabledAsync(int id,int version,bool enabled,int? organisationId=null) => api.InvokeAsync(typeof(HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations),nameof(SetEnabledAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(version),HRM.Application.Transport.ApiArgument.From(enabled),HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task EndAsync(int id,int version,DateTime lastDay,int? organisationId=null) => api.InvokeAsync(typeof(HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations),nameof(EndAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(version),HRM.Application.Transport.ApiArgument.From(lastDay),HRM.Application.Transport.ApiArgument.From(organisationId)],default);
}
