using HRM.DTOs.Leave;

namespace HRM.Services.Interfaces.LeaveType
{
    public interface ILeaveTypeService
    {
        Task<List<LeaveTypeDto>> GetOrganisationLeaveTypesAsync(
       CancellationToken cancellationToken = default);

        Task<LeaveTypeDto?> GetByIdAsync(
            int leaveTypeId,
            CancellationToken cancellationToken = default);

        Task<LeaveTypeSaveResult> CreateAsync(
            LeaveTypeSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<LeaveTypeSaveResult> UpdateAsync(
            LeaveTypeSaveRequest request,
            CancellationToken cancellationToken = default);
    }
}
