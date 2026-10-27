using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class JobTypeConfigurationServiceContracts
{

public interface IOperations
{
Task<JobTypeConfigurationData> LoadAsync(bool requestTypes = false)
;
Task<ServiceResult> SaveJobTypeAsync(JobTypeConfigurationDto model)
;
Task<ServiceResult> SaveRequestTypeAsync(JobTypeConfigurationDto model)
;
Task<ServiceResult> DeleteRequestTypeAsync(int id)
;
Task<JobTypeWorkflowDto> GetWorkflowAsync(int jobTypeId, int requestTypeId, bool requestTypeDefault = false)
;
Task<ServiceResult> SaveWorkflowAsync(JobTypeWorkflowDto model, bool requestTypeDefault = false)
;
}
}
