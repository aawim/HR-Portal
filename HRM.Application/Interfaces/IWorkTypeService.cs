using HRM.DTOs;
using HRM.DTOs.WorkTypes;

namespace HRM.Services.Interfaces
{
    public interface IWorkTypeService
    {
        Task<bool> CanClockAsync();
        Task<List<WorkTypeStaffOption>> GetEligibleStaffAsync();
        Task<List<WorkTypeDto>> GetAllAsync(bool includeInactive = false);

        Task<WorkTypeDto?> GetByIdAsync(int workTypeId);

        Task<ServiceResult> CreateAsync(CreateWorkTypeDto dto);

        Task<ServiceResult> UpdateAsync(UpdateWorkTypeDto dto);

        Task<ServiceResult> SetActiveAsync(int workTypeId, bool isActive);

        Task<List<AssignedWorkTypeDto>> GetAssignmentsAsync(int? jobId = null, int? organisationId = null, bool includeInactive = false);

        Task<AssignedWorkTypeDto?> GetAssignmentByIdAsync(int assignedWorkTypeId);

        Task<ServiceResult> AssignAsync(CreateAssignedWorkTypeDto dto);

        Task<ServiceResult> UpdateAssignmentAsync(UpdateAssignedWorkTypeDto dto);

        Task<ServiceResult> DeactivateAssignmentAsync(int assignedWorkTypeId);
    }
}
