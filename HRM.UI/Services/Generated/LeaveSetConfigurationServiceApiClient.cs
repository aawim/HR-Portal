using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class LeaveSetConfigurationServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.LeaveSetConfigurationServiceContracts, HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations
{
public Task<LeaveSetContentsData> LoadContentsAsync(int id) => api.InvokeAsync<LeaveSetContentsData>(typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),nameof(LoadContentsAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
public Task SaveContentsAsync(int id,IReadOnlyCollection<int> typeIds,int expectedAudit) => api.InvokeAsync(typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),nameof(SaveContentsAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(typeIds),HRM.Application.Transport.ApiArgument.From(expectedAudit)],default);
public Task<JobTypeLeaveSetData> LoadJobTypeAsync(int jobTypeId) => api.InvokeAsync<JobTypeLeaveSetData>(typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),nameof(LoadJobTypeAsync), [HRM.Application.Transport.ApiArgument.From(jobTypeId)],default);
public Task SaveJobTypeAsync(int jobTypeId,IReadOnlyCollection<int> setIds,string expectedVersion) => api.InvokeAsync(typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),nameof(SaveJobTypeAsync), [HRM.Application.Transport.ApiArgument.From(jobTypeId),HRM.Application.Transport.ApiArgument.From(setIds),HRM.Application.Transport.ApiArgument.From(expectedVersion)],default);
public Task<global::HRM.DTOs.Contracts.LeaveSetConfigurationDataResponse> LoadAsync() => api.InvokeAsync<global::HRM.DTOs.Contracts.LeaveSetConfigurationDataResponse>(typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task SaveAsync(global::HRM.DTOs.Contracts.LeaveSetDto model) => api.InvokeAsync(typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),nameof(SaveAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task DeleteAsync(global::HRM.DTOs.Contracts.LeaveSetDto model) => api.InvokeAsync(typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),nameof(DeleteAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
}
