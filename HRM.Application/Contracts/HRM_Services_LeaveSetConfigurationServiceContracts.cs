using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class LeaveSetConfigurationServiceContracts
{

public interface IOperations
{
Task<LeaveSetContentsData> LoadContentsAsync(int id)
;
Task SaveContentsAsync(int id,IReadOnlyCollection<int> typeIds,int expectedAudit)
;
Task<JobTypeLeaveSetData> LoadJobTypeAsync(int jobTypeId)
;
Task SaveJobTypeAsync(int jobTypeId,IReadOnlyCollection<int> setIds,string expectedVersion)
;
Task<global::HRM.DTOs.Contracts.LeaveSetConfigurationDataResponse> LoadAsync()
;
Task SaveAsync(global::HRM.DTOs.Contracts.LeaveSetDto model)
;
Task DeleteAsync(global::HRM.DTOs.Contracts.LeaveSetDto model)
;
}
}
