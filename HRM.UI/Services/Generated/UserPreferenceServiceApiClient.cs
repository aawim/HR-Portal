using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class UserPreferenceServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.UserPreferenceServiceContracts, HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations
{
public Task<UserThemePreference> GetThemeAsync() => api.InvokeAsync<UserThemePreference>(typeof(HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations),nameof(GetThemeAsync), [],default);
public Task SaveThemeAsync(string theme, int expectedUserId) => api.InvokeAsync(typeof(HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations),nameof(SaveThemeAsync), [HRM.Application.Transport.ApiArgument.From(theme),HRM.Application.Transport.ApiArgument.From(expectedUserId)],default);
public Task SaveFontSizeAsync(int percent, int expectedUserId) => api.InvokeAsync(typeof(HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations),nameof(SaveFontSizeAsync), [HRM.Application.Transport.ApiArgument.From(percent),HRM.Application.Transport.ApiArgument.From(expectedUserId)],default);
}
