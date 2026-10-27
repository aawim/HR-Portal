using HRM.Services.Transport;
namespace HRM.Services;
public partial class JobTypeOvertimeSettingsService
{
    async global::System.Threading.Tasks.Task<global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto> global::HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations.LoadAsync(global::System.Int32 jobTypeId)
    {
        var result = await LoadAsync(jobTypeId);
        return ContractDtoMappings.ToDto(result);
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations.SaveAsync(global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto model)
    {
        await SaveAsync(ContractDtoMappings.ToDomain(model));
    }

}
