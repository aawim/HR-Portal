using HRM.DTOs.Leave;
using HRM.DTOs;
using HRM.DTOs.LeaveTypes;

namespace HRM.Services.Interfaces.JobLeaveTypes
{
    public interface IJobLeaveTypeService
    {
        //Task<List<JobLeaveTypeDto>> GetAssignedAsync(int jobId);

        //Task<List<LeaveTypeDto>> GetAvailableAsync(int jobId);

        //Task<ServiceResult> AssignAsync(int jobId, int leaveTypeId);

        //Task<ServiceResult> AssignAsync(AssignLeaveTypeDto dto);
        Task<ServiceResult> UpdateAsync(JobLeaveTypeEditDto dto);
        Task<List<JobLeaveTypeDto>> GetJobLeaveTypeByJobId(int StaffId);


        //Task<ServiceResult> RemoveAsync(int jobLeaveTypeId);

        Task<ServiceResult> AssignLegacyAsync(AssignLegacyLeaveTypeDto request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> AssignDefinitionAsync(AssignLeaveDefinitionDto request,
            CancellationToken cancellationToken = default);
    }
}
