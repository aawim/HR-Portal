using HRM.Services.Transport;
namespace HRM.Services;
public partial class ScopeScheduleSettingsService
{
    async global::System.Threading.Tasks.Task<global::HRM.DTOs.Contracts.ScopeScheduleSettingsDataResponse> global::HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations.LoadAsync(global::System.Int32? organisationId)
    {
        var result = await LoadAsync(organisationId);
        return ContractDtoMappings.ToDto(result);
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations.SaveAsync(global::HRM.DTOs.Contracts.ScopeScheduleAssignmentDto model, global::System.Int32? organisationId)
    {
        await SaveAsync(ContractDtoMappings.ToDomain(model), organisationId);
    }

}
