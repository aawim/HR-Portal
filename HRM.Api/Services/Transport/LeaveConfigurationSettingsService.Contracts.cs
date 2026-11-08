using HRM.Services.Transport;
namespace HRM.Services;
public partial class LeaveConfigurationSettingsService
{
    async global::System.Threading.Tasks.Task<global::HRM.DTOs.Contracts.LeaveConfigurationDataResponse> global::HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations.LoadAsync()
    {
        var result = await LoadAsync();
        return ContractDtoMappings.ToDto(result);
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations.SaveAsync(global::HRM.DTOs.Contracts.LeaveConfigurableValueDto model)
    {
        await SaveAsync(ContractDtoMappings.ToDomain(model));
    }

}
