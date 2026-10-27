using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.DTOs.Contracts;
using HRM.Services.Interfaces.Leave;
namespace HRM.Application.Contracts.Pages;

/// <summary>DTO-only boundary for staff and authorised HR-on-behalf leave applications.</summary>
public interface ILeaveApplications
{
    Task<int> GetJobIdByStaffId(int staffId);
    Task<List<LeaveReasonDto>> GetReasonsByLeaveType(int? leaveTypeId);
    Task<LeaveApplicationTypeDto?> GetApplicationLeaveTypeAsync(int jobLeaveTypeId, int? applicantJobId = null, DateTime? date = null);
    Task<LeaveHandoverRulesDto> GetHandoverRulesAsync(int jobLeaveTypeId, DateTime fromDate, int? applicantJobId = null);
    Task<List<LeaveApplicantJob>> GetLeaveApplicantJobsAsync(int staffId);
    Task<List<HRM.DTOs.Leave.JobLeaveTypeDto>> GetApplicantLeaveBalancesAsync(int applicantJobId);
    Task<List<HRM.DTOs.Leave.JobLeaveTypeDto>> GetMyLeaveBalancesAsync(int? leaveTypeId = 0);
    Task<List<HRM.DTOs.Leave.JobLeaveTypeDto>> GetJobLeaveBalancesAsync(int jobId, int? leaveTypeId = 0);
    Task<List<LeafDto>> GetLeaveRequestsAsync(int jobId);
    Task<List<HRM.DTOs.Leave.JobLeaveTypeDto>> GetJobLeaveTypesAsync();
    Task<List<LeafDto>> GetMyLeaveRequestsAsync(int? statusId = null, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null);
    Task<ServiceResult> SubmitLeave(LeaveApplicationDto model, int? applicantJobId = null);
    Task<ServiceResult> CancelLeaveAsync(int leaveId);
    Task<List<ProcessingLeaveDto>> GetMyProcessingLeavesAsync();
}
