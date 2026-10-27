using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.UI.Services.Generated;
public sealed class StaffOnboardingServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.StaffOnboardingServiceContracts, HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations
{
public Task<StaffOnboardingData> LoadAsync(int? individualId = null) => api.InvokeAsync<StaffOnboardingData>(typeof(HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(individualId)],default);
public Task<ServiceResult> SubmitAsync(StaffOnboardingForm model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations),nameof(SubmitAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<List<StaffOnboardingRow>> GetRequestsAsync(int? requestId = null, bool pendingOnly = false) => api.InvokeAsync<List<StaffOnboardingRow>>(typeof(HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations),nameof(GetRequestsAsync), [HRM.Application.Transport.ApiArgument.From(requestId),HRM.Application.Transport.ApiArgument.From(pendingOnly)],default);
public Task<ServiceResult> ActAsync(int requestId, int expectedState, string action, string? comment) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations),nameof(ActAsync), [HRM.Application.Transport.ApiArgument.From(requestId),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
}
