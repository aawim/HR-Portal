using HRM.Services.Transport;
namespace HRM.Services.Leave;
public partial class LeaveAccrualSettingsService
{
    async global::System.Threading.Tasks.Task<global::HRM.DTOs.Contracts.LeaveAccrualSettingsDataResponse> global::HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations.LoadAsync()
    {
        var result = await LoadAsync();
        return ContractDtoMappings.ToDto(result);
    }

    async global::System.Threading.Tasks.Task global::HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations.SaveAsync(global::HRM.DTOs.Contracts.JobTypeLeaveAccrualRuleDto model)
    {
        await SaveAsync(ContractDtoMappings.ToDomain(model));
    }

}
