using System.Data;
using HRM.Services.Interfaces;
using HRM.Components.Shared;
namespace HRM.Services.Contracts;
public abstract class LeaveConfigurationSettingsServiceContracts
{

public interface IOperations
{
Task<global::HRM.DTOs.Contracts.LeaveConfigurationDataResponse> LoadAsync()
;
Task SaveAsync(global::HRM.DTOs.Contracts.LeaveConfigurableValueDto model)
;
Task DeleteAsync(int id)
;
}
}
