using HRM.Services;
using HRM.Services.Contracts;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.DTOs.UserContext;
using HRM.DTOs;
namespace HRM.UI.Services.Generated;
public sealed class UserServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.UserServiceContracts, HRM.Services.Contracts.UserServiceContracts.IOperations
{
public Task<UserProfileDto?> GetByIdAsync(int userId) => api.InvokeAsync<UserProfileDto?>(typeof(HRM.Services.Contracts.UserServiceContracts.IOperations),nameof(GetByIdAsync), [HRM.Application.Transport.ApiArgument.From(userId)],default);
public Task<UserProfileDto?> GetByUsernameAsync(string username) => api.InvokeAsync<UserProfileDto?>(typeof(HRM.Services.Contracts.UserServiceContracts.IOperations),nameof(GetByUsernameAsync), [HRM.Application.Transport.ApiArgument.From(username)],default);
public Task<UserProfileDto?> GetByBusinessEntityAsync(int businessEntityId) => api.InvokeAsync<UserProfileDto?>(typeof(HRM.Services.Contracts.UserServiceContracts.IOperations),nameof(GetByBusinessEntityAsync), [HRM.Application.Transport.ApiArgument.From(businessEntityId)],default);
public Task<UserProfileDto?> GetMyProfileAsync() => api.InvokeAsync<UserProfileDto?>(typeof(HRM.Services.Contracts.UserServiceContracts.IOperations),nameof(GetMyProfileAsync), [],default);
public Task<bool> UpdateUserProfileAsync(UserProfileDto updatedUser) => api.InvokeAsync<bool>(typeof(HRM.Services.Contracts.UserServiceContracts.IOperations),nameof(UpdateUserProfileAsync), [HRM.Application.Transport.ApiArgument.From(updatedUser)],default);
public Task<bool> UpdateUserAsync(UserProfileDto profile) => api.InvokeAsync<bool>(typeof(HRM.Services.Contracts.UserServiceContracts.IOperations),nameof(UpdateUserAsync), [HRM.Application.Transport.ApiArgument.From(profile)],default);
}
