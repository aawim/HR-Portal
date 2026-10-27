using HRM.WorkPlanning.Services;
using HRM.WorkPlanning.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.WorkPlanning;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Enum;
using HRM.Models.Workflow;
namespace HRM.UI.Services.Generated;
public sealed class DutyRosterServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts, HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations
{
public Task<List<DutyPartner>> GetPartnersAsync() => api.InvokeAsync<List<DutyPartner>>(typeof(HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations),nameof(GetPartnersAsync), [],default);
public Task<List<DutyRosterRow>> GetRosterAsync(DateTime date) => api.InvokeAsync<List<DutyRosterRow>>(typeof(HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations),nameof(GetRosterAsync), [HRM.Application.Transport.ApiArgument.From(date)],default);
public Task<ServiceResult> SubmitAsync(DutyChangeApplication model) => api.InvokeAsync<ServiceResult>(typeof(HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations),nameof(SubmitAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<List<DutyRequestRow>> GetRequestsAsync() => api.InvokeAsync<List<DutyRequestRow>>(typeof(HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations),nameof(GetRequestsAsync), [],default);
public Task<HRM.DTOs.ServiceResult> ActAsync(long id, int expectedState, string action, string? comment = null) => api.InvokeAsync<HRM.DTOs.ServiceResult>(typeof(HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations),nameof(ActAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
}
