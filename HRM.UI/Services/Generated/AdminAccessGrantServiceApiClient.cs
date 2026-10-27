using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using System.Text.Json;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.UI.Services.Generated;
public sealed class AdminAccessGrantServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.AdminAccessGrantServiceContracts, HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations
{
public Task<AdminAccessGrantData> LoadAsync(int? organisationId = null) => api.InvokeAsync<AdminAccessGrantData>(typeof(HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(organisationId)],default);
public Task<ServiceResult> SubmitAsync(int organisationId, int userId, IEnumerable<int> groupIds, IEnumerable<int> permissionIds, string reason) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations),nameof(SubmitAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(userId),HRM.Application.Transport.ApiArgument.From(groupIds),HRM.Application.Transport.ApiArgument.From(permissionIds),HRM.Application.Transport.ApiArgument.From(reason)],default);
public Task<ServiceResult> ActAsync(int organisationId, int id, int expectedState, string action, string? comment) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations),nameof(ActAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
public Task<ServiceResult> RevokeAsync(int organisationId, int userId, string reason) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations),nameof(RevokeAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(userId),HRM.Application.Transport.ApiArgument.From(reason)],default);
}
