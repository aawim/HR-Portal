using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.UI.Services.Generated;
public sealed class WeeklyHourExceptionServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.WeeklyHourExceptionServiceContracts, HRM.Services.Contracts.WeeklyHourExceptionServiceContracts.IOperations
{
public Task<ServiceResult> SubmitAsync(DateTime day, int minutes, string reason) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.WeeklyHourExceptionServiceContracts.IOperations),nameof(SubmitAsync), [HRM.Application.Transport.ApiArgument.From(day),HRM.Application.Transport.ApiArgument.From(minutes),HRM.Application.Transport.ApiArgument.From(reason)],default);
public Task<List<WeeklyHourExceptionRow>> LoadAsync(bool pendingOnly) => api.InvokeAsync<List<WeeklyHourExceptionRow>>(typeof(HRM.Services.Contracts.WeeklyHourExceptionServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(pendingOnly)],default);
public Task<ServiceResult> ActAsync(int id, int expectedState, string action, string? comment) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.WeeklyHourExceptionServiceContracts.IOperations),nameof(ActAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
}
