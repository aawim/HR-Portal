using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using System.Text.Json;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.DTOs;
namespace HRM.UI.Services.Generated;
public sealed class StaffPositionManagementServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.StaffPositionManagementServiceContracts, HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations
{
public Task<Data> LoadAsync(int staffId) => api.InvokeAsync<Data>(typeof(HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(staffId)],default);
public Task SaveAsync(int staffId, int jobId, int positionId, DateTime from, DateTime? to,
        string reason, Assignment? original = null, bool change = false, bool remove = false) => api.InvokeAsync(typeof(HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(staffId),HRM.Application.Transport.ApiArgument.From(jobId),HRM.Application.Transport.ApiArgument.From(positionId),HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to),HRM.Application.Transport.ApiArgument.From(reason),HRM.Application.Transport.ApiArgument.From(original),HRM.Application.Transport.ApiArgument.From(change),HRM.Application.Transport.ApiArgument.From(remove)],default);
public Task<List<StaffPositionRequestRow>> LoadRequestsAsync(int? staffId = null, bool pendingOnly = false) => api.InvokeAsync<List<StaffPositionRequestRow>>(typeof(HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations),nameof(LoadRequestsAsync), [HRM.Application.Transport.ApiArgument.From(staffId),HRM.Application.Transport.ApiArgument.From(pendingOnly)],default);
public Task<ServiceResult> ActOnRequestAsync(int id, int expectedState, string action, string? comment) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations),nameof(ActOnRequestAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment)],default);
}
