using System.Data;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave.Contracts;
public abstract class LeaveAccrualSettingsServiceContracts
{

public interface IOperations
{
Task<global::HRM.DTOs.Contracts.LeaveAccrualSettingsDataResponse> LoadAsync()
;
Task SaveAsync(global::HRM.DTOs.Contracts.JobTypeLeaveAccrualRuleDto model)
;
Task<List<AccrualPreview>> PreviewAsync(int ruleId)
;
Task<int> RunAsync(int ruleId)
;
}
}
