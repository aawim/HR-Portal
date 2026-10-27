using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.Leave;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave.Contracts;
public abstract class LeaveFlowConfigurationServiceContracts
{

public interface IOperations
{
Task<LeaveFlowSettingsData> LoadAsync()
;
Task<LeaveFlowConfigurationDto> GetAsync(int jobType, int requestType)
;
Task<ServiceResult> SaveAsync(LeaveFlowConfigurationDto model)
;
}
}
