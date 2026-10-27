using System.Data;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class JobTypeOvertimeSettingsServiceContracts
{

public interface IOperations
{
Task<global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto> LoadAsync(int jobTypeId)
;
Task SaveAsync(global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto model)
;
}
}
