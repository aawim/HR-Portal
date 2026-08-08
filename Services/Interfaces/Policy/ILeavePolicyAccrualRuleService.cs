using HRM.DTOs.LeaveTypePolicy;
using HRM.DTOs.LeaveTypes;

namespace HRM.Services.Interfaces.Policy
{
    public interface ILeavePolicyAccrualRuleService
    {
        Task<List<LeavePolicyAccrualRuleDto>> GetByPolicyIdAsync(
        int leavePolicyId,
        CancellationToken cancellationToken = default);

        Task<LeavePolicyAccrualRuleDto?> GetActiveRuleAsync(
            int leavePolicyId,
            CancellationToken cancellationToken = default);

        Task<LeavePolicyAccrualRuleDto?> GetByIdAsync(
            int leavePolicyAccrualRuleId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> CreateAsync(
            LeavePolicyAccrualRuleSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> UpdateAsync(
            LeavePolicyAccrualRuleSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> DeactivateAsync(
            int leavePolicyAccrualRuleId,
            CancellationToken cancellationToken = default);
    }
}
