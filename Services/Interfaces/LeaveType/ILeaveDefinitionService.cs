using HRM.DTOs.LeaveTypes;

namespace HRM.Services.Interfaces.LeaveType
{
    public interface ILeaveDefinitionService
    {
        Task<List<LeaveDefinitionDto>> GetAvailableAsync(CancellationToken cancellationToken = default);

        Task<LeaveDefinitionDto?> GetByIdAsync(
            int leaveDefinitionId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> CreateAsync(
            LeaveDefinitionSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> UpdateAsync(
            LeaveDefinitionSaveRequest request,
            CancellationToken cancellationToken = default);
    }
}
