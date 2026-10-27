using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using System.Text.Json;
using HRM.DTOs;
using HRM.Enum;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class WorkspaceRecentSearchServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts, HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts.IOperations
{
public Task<WorkspaceSearchResult?> ResolveAsync(WorkspaceEntityType type, int id) => api.InvokeAsync<WorkspaceSearchResult?>(typeof(HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts.IOperations),nameof(ResolveAsync), [HRM.Application.Transport.ApiArgument.From(type),HRM.Application.Transport.ApiArgument.From(id)],default);
public Task<List<WorkspaceSearchResult>> GetAsync() => api.InvokeAsync<List<WorkspaceSearchResult>>(typeof(HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts.IOperations),nameof(GetAsync), [],default);
public Task<WorkspaceSearchResult?> RememberAsync(WorkspaceSearchResult selected) => api.InvokeAsync<WorkspaceSearchResult?>(typeof(HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts.IOperations),nameof(RememberAsync), [HRM.Application.Transport.ApiArgument.From(selected)],default);
}
