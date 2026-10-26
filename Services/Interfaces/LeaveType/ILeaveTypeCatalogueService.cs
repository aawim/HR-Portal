using HRM.DTOs.LeaveTypes;

namespace HRM.Services.Interfaces.LeaveType
{
    public interface ILeaveTypeCatalogueService
    {
        Task<List<LeaveRequestTypeOption>> GetRequestTypesAsync(CancellationToken cancellationToken = default);

        Task<List<LeaveTypeCatalogueDto>> GetManagementAsync(CancellationToken cancellationToken = default);

        Task<List<LeaveTypeCatalogueDto>> GetAvailableAsync(CancellationToken cancellationToken = default);

        Task<LeaveTypeCatalogueDto?> GetByIdAsync(
            int leaveDefinitionId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> CreateAsync(
            LeaveTypeCatalogueSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> UpdateAsync(
            LeaveTypeCatalogueSaveRequest request,
            CancellationToken cancellationToken = default);
    }
}
