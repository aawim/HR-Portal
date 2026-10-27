using HRM.Services.Transport;
namespace HRM.Services;
public partial class WorkingHoursSettingsService
{
    async global::System.Threading.Tasks.Task<global::HRM.DTOs.Contracts.WorkingHoursSettingsDataResponse> global::HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations.LoadAsync()
    {
        var result = await LoadAsync();
        return ContractDtoMappings.ToDto(result);
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations.SaveRuleAsync(global::HRM.DTOs.Contracts.JobTypeWorkingHourRuleDto model)
    {
        await SaveRuleAsync(ContractDtoMappings.ToDomain(model));
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations.SaveRequirementAsync(global::HRM.DTOs.Contracts.OrganisationWorkingHourRequirementDto model)
    {
        await SaveRequirementAsync(ContractDtoMappings.ToDomain(model));
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations.SaveAmendmentAsync(global::HRM.DTOs.Contracts.OrganisationScheduleAmendmentDto model)
    {
        await SaveAmendmentAsync(ContractDtoMappings.ToDomain(model));
    }

}
