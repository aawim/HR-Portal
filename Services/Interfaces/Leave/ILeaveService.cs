using HRM.Models;
using HRM.DTOs.Leave;
using HRM.DTOs;
using HRM.DTOs.LeaveTypes;

namespace HRM.Services.Interfaces.Leave
{
    public interface ILeaveService
    {

        Task<int> GetJobIdByStaffId(int StaffId);
        //Task<List<JobLeaveTypeDto>> GetJobLeaveTypeByJobId(int jobId);
        Task<List<LeaveReasonDto>> GetReasonsByLeaveType(int? leaveTypeId);
        Task<List<LeaveReasonDto>> GetReasonsByLeaveType();
   
        Task<List<JobLeaveType>> GetMyLeaveBalancesAsync(int? leaveTypeId = 0);
        Task<List<JobLeaveType>> GetJobLeaveBalancesAsync(int jobId,int? leaveTypeId = 0);

        Task<List<Leaf>>GetLeaveRequestsAsync(int jobId);
        Task<List<JobLeaveType>> GetJobLeaveTypesAsync();

        Task<List<Leaf>> GetMyLeaveRequestsAsync();


        Task<ServiceResult> SubmitLeave(LeaveApplicationDto model);
        Task<ServiceResult> CancelLeaveAsync(int leaveId);
        Task<ServiceResult> ShowDetailLeaveAsync(int? leaveId);
        Task<List<ProcessingLeaveDto>> GetMyProcessingLeavesAsync();

 







    }
}
