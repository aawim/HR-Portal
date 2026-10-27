using System.Data;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class ScopeScheduleSettingsServiceContracts
{

public interface IOperations
{
Task<global::HRM.DTOs.Contracts.ScopeScheduleSettingsDataResponse> LoadAsync(int? organisationId=null)
;
Task SaveAsync(global::HRM.DTOs.Contracts.ScopeScheduleAssignmentDto model,int? organisationId=null)
;
Task SetEnabledAsync(int id,int version,bool enabled,int? organisationId=null)
;
Task EndAsync(int id,int version,DateTime lastDay,int? organisationId=null)
;
}
}
