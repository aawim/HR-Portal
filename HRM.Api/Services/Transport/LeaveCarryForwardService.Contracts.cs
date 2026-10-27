using HRM.Services.Transport;
namespace HRM.Services.Leave;
public partial class LeaveCarryForwardService
{
    async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.LeaveCarryForwardDto>> global::HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations.HistoryAsync()
    {
        var result = await HistoryAsync();
        return result.Select(item => ContractDtoMappings.ToDto(item)).ToList();
    }

}
