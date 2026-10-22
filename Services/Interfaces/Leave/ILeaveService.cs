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
        Task<LeaveApplicationTypeDto?> GetApplicationLeaveTypeAsync(int jobLeaveTypeId, int? applicantJobId = null);
        Task<LeaveHandoverRulesDto> GetHandoverRulesAsync(int jobLeaveTypeId, DateTime fromDate, int? applicantJobId = null);
        Task<List<LeaveApplicantJob>> GetLeaveApplicantJobsAsync(int staffId);
        Task<List<JobLeaveType>> GetApplicantLeaveBalancesAsync(int applicantJobId);
        Task<List<LeaveReasonDto>> GetReasonsByLeaveType();
   
        Task<List<JobLeaveType>> GetMyLeaveBalancesAsync(int? leaveTypeId = 0);
        Task<List<JobLeaveType>> GetJobLeaveBalancesAsync(int jobId,int? leaveTypeId = 0);

        Task<List<Leaf>>GetLeaveRequestsAsync(int jobId);
        Task<List<JobLeaveType>> GetJobLeaveTypesAsync();

        Task<List<Leaf>> GetMyLeaveRequestsAsync(int? statusId = null, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null);


        Task<ServiceResult> SubmitLeave(LeaveApplicationDto model, int? applicantJobId = null);
        Task<ServiceResult> CancelLeaveAsync(int leaveId);
        Task<ServiceResult> ShowDetailLeaveAsync(int? leaveId);
        Task<List<ProcessingLeaveDto>> GetMyProcessingLeavesAsync();

 







    }
}
