using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class PermissionSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.PermissionSettingsServiceContracts, HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations
{
public Task<PermissionSettingsData> LoadAsync(bool groups) => api.InvokeAsync<PermissionSettingsData>(typeof(HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(groups)],default);
public Task<ServiceResult> SavePermissionAsync(PermissionDefinitionDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations),nameof(SavePermissionAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> SaveRoleAsync(AccessRoleDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations),nameof(SaveRoleAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
}
