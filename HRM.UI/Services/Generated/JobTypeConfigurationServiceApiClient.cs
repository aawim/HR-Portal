using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class JobTypeConfigurationServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.JobTypeConfigurationServiceContracts, HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations
{
public Task<JobTypeConfigurationData> LoadAsync(bool requestTypes = false) => api.InvokeAsync<JobTypeConfigurationData>(typeof(HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(requestTypes)],default);
public Task<ServiceResult> SaveJobTypeAsync(JobTypeConfigurationDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations),nameof(SaveJobTypeAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> SaveRequestTypeAsync(JobTypeConfigurationDto model) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations),nameof(SaveRequestTypeAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
public Task<ServiceResult> DeleteRequestTypeAsync(int id) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations),nameof(DeleteRequestTypeAsync), [HRM.Application.Transport.ApiArgument.From(id)],default);
public Task<JobTypeWorkflowDto> GetWorkflowAsync(int jobTypeId, int requestTypeId, bool requestTypeDefault = false) => api.InvokeAsync<JobTypeWorkflowDto>(typeof(HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations),nameof(GetWorkflowAsync), [HRM.Application.Transport.ApiArgument.From(jobTypeId),HRM.Application.Transport.ApiArgument.From(requestTypeId),HRM.Application.Transport.ApiArgument.From(requestTypeDefault)],default);
public Task<ServiceResult> SaveWorkflowAsync(JobTypeWorkflowDto model, bool requestTypeDefault = false) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations),nameof(SaveWorkflowAsync), [HRM.Application.Transport.ApiArgument.From(model),HRM.Application.Transport.ApiArgument.From(requestTypeDefault)],default);
}
