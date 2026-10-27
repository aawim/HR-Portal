using System.Data;
using System.Text.Json;
using HRM.DTOs.Leave;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave.Contracts;
public abstract class LeaveHandoverRuleSettingsServiceContracts
{

public interface IOperations
{
Task<HandoverRuleSettingsData> LoadAsync()
;
Task<List<HandoverOption>> StaffAsync(string search)
;
Task<EffectiveHandoverRule> PreviewAsync(int jobId,int typeId,DateTime date)
;
Task SaveAsync(HandoverRuleEdit m)
;
}
}
