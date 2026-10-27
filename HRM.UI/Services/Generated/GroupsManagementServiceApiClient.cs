using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class GroupsManagementServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.GroupsManagementServiceContracts, HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations
{
public Task<OperationalGroupsData> LoadAsync(int? selected = null) => api.InvokeAsync<OperationalGroupsData>(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(selected)],default);
public Task<int> RequireGroupAsync(int id, bool write = false) => api.InvokeAsync<int>(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(RequireGroupAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(write)],default);
public Task<bool> CanManageGroupAsync(int id) => api.InvokeAsync<bool>(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(CanManageGroupAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
public Task SaveGroupAsync(int org, int id, string name, int type, int structure, int expectedAudit) => api.InvokeAsync(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(SaveGroupAsync), [HRM.Application.Transport.ApiArgument.From(org),HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(name),HRM.Application.Transport.ApiArgument.From(type),HRM.Application.Transport.ApiArgument.From(structure),HRM.Application.Transport.ApiArgument.From(expectedAudit)],default);
public Task DeleteGroupAsync(int org, int id, int expectedAudit) => api.InvokeAsync(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(DeleteGroupAsync), [HRM.Application.Transport.ApiArgument.From(org),HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedAudit)],default);
public Task SaveTypeAsync(int org, int id, string name, string? originalName) => api.InvokeAsync(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(SaveTypeAsync), [HRM.Application.Transport.ApiArgument.From(org),HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(name),HRM.Application.Transport.ApiArgument.From(originalName)],default);
public Task DeleteTypeAsync(int org,int id,string originalName) => api.InvokeAsync(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(DeleteTypeAsync), [HRM.Application.Transport.ApiArgument.From(org),HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(originalName)],default);
public Task AssignLeaveSetAsync(int groupId,int leaveSetId) => api.InvokeAsync(typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),nameof(AssignLeaveSetAsync), [HRM.Application.Transport.ApiArgument.From(groupId),HRM.Application.Transport.ApiArgument.From(leaveSetId)],default);
}
