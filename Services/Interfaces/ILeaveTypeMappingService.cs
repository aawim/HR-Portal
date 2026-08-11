using HRM.DTOs.LeaveTypes;

namespace HRM.Services.Interfaces
{
    public interface ILeaveTypeMappingService
    {
        Task<LeaveTypeMappingDto?> GetByLegacyLeaveTypeAsync(
       int? legacyLeaveTypeId,
       CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> SetMappingAsync(
            LeaveTypeMappingSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> RemoveMappingAsync(
            int? legacyLeaveTypeId,
            CancellationToken cancellationToken = default);
    }
}
