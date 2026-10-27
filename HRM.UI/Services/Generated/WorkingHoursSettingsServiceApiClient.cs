using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class WorkingHoursSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.WorkingHoursSettingsServiceContracts, HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations
{
public Task<global::HRM.DTOs.Contracts.WorkingHoursSettingsDataResponse> LoadAsync() => api.InvokeAsync<global::HRM.DTOs.Contracts.WorkingHoursSettingsDataResponse>(typeof(HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task SaveRuleAsync(global::HRM.DTOs.Contracts.JobTypeWorkingHourRuleDto model) => api.InvokeAsync(typeof(HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations),nameof(SaveRuleAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task SaveRequirementAsync(global::HRM.DTOs.Contracts.OrganisationWorkingHourRequirementDto model) => api.InvokeAsync(typeof(HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations),nameof(SaveRequirementAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task SaveAmendmentAsync(global::HRM.DTOs.Contracts.OrganisationScheduleAmendmentDto model) => api.InvokeAsync(typeof(HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations),nameof(SaveAmendmentAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
}
