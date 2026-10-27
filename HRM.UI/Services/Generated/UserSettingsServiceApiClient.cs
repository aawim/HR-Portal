using HRM.Services;
using HRM.Services.Contracts;
using System.ComponentModel.DataAnnotations;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class UserSettingsServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.UserSettingsServiceContracts, HRM.Services.Contracts.UserSettingsServiceContracts.IOperations
{
public Task<UserSettingsData> LoadAsync(UserSettingsTab tab) => api.InvokeAsync<UserSettingsData>(typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(tab)],default);
public Task<ServiceResult> SaveAccountAsync(ManagedUserDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),nameof(SaveAccountAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> SaveLegacyProfileAsync(UserProfileDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),nameof(SaveLegacyProfileAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> SaveUserAccessAsync(ManagedUserDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),nameof(SaveUserAccessAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> SaveStaffAsync(ManagedStaffDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),nameof(SaveStaffAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> SaveEnrollmentAsync(ManagedEnrollmentDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),nameof(SaveEnrollmentAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> SaveOrganisationAccessAsync(List<int> permissionIds) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),nameof(SaveOrganisationAccessAsync), [HRM.Application.Transport.ApiArgument.From(permissionIds)],default);
}
