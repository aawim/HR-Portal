using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class RequestTransitionSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.RequestTransitionSettingsServiceContracts, HRM.Services.Contracts.RequestTransitionSettingsServiceContracts.IOperations
{
public Task<RequestTransitionSettingsDto> LoadAsync() => api.InvokeAsync<RequestTransitionSettingsDto>(typeof(HRM.Services.Contracts.RequestTransitionSettingsServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task<List<AllowedRequestTransitionDto>> GetAsync(int requestType) => api.InvokeAsync<List<AllowedRequestTransitionDto>>(typeof(HRM.Services.Contracts.RequestTransitionSettingsServiceContracts.IOperations),nameof(GetAsync), [HRM.Application.Transport.ApiArgument.From(requestType)],default);
public Task<ServiceResult> SetAsync(int requestType, int from, int to, bool allowed) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.RequestTransitionSettingsServiceContracts.IOperations),nameof(SetAsync), [HRM.Application.Transport.ApiArgument.From(requestType),HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to),HRM.Application.Transport.ApiArgument.From(allowed)],default);
}
