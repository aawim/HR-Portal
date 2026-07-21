using HRM.DTOs.Leave;
using HRM.DTOs;

namespace HRM.Services.Interfaces
{
    public interface IJobLeaveTypeService
    {
        //Task<List<JobLeaveTypeDto>> GetAssignedAsync(int jobId);

        //Task<List<LeaveTypeDto>> GetAvailableAsync(int jobId);

        //Task<ServiceResult> AssignAsync(int jobId, int leaveTypeId);
        Task<ServiceResult> AssignAsync(AssignLeaveTypeDto dto);


        Task<ServiceResult> UpdateAsync(JobLeaveTypeEditDto dto);



        //Task<ServiceResult> RemoveAsync(int jobLeaveTypeId);
    }
}
