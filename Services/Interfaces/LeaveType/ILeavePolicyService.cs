using HRM.DTOs.LeaveTypes;

namespace HRM.Services.Interfaces.LeaveType
{
    public interface ILeavePolicyService
    {
        Task<List<LeavePolicyDto>> GetOrganisationPoliciesAsync(
             CancellationToken cancellationToken = default);

        Task<LeavePolicyDto?> GetByIdAsync(
            int policyId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> CreateAsync(
            LeavePolicySaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> UpdateAsync(
            LeavePolicySaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> DeactivateAsync(
            int policyId,
            CancellationToken cancellationToken = default);
    }
}
