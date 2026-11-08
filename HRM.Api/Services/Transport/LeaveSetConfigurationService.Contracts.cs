using HRM.Services.Transport;
namespace HRM.Services;
public partial class LeaveSetConfigurationService
{
    async global::System.Threading.Tasks.Task<global::HRM.DTOs.Contracts.LeaveSetConfigurationDataResponse> global::HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations.LoadAsync()
    {
        var result = await LoadAsync();
        return ContractDtoMappings.ToDto(result);
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations.SaveAsync(global::HRM.DTOs.Contracts.LeaveSetDto model)
    {
        await SaveAsync(ContractDtoMappings.ToDomain(model));
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations.DeleteAsync(global::HRM.DTOs.Contracts.LeaveSetDto model)
    {
        await DeleteAsync(ContractDtoMappings.ToDomain(model));
    }

}
