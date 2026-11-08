using HRM.Application.Contracts.Pages;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.Services.Interfaces.Leave;
using LeaveSummary = HRM.DTOs.Contracts.LeafDto;

namespace HRM.Services.Transport;

/// <summary>Projects the existing leave workflow into DTOs without duplicating submission or approval logic.</summary>
public sealed class LeaveApplicationsService(ILeaveService leaves) : ILeaveApplications
{
    public Task<int> GetJobIdByStaffId(int staffId) => leaves.GetJobIdByStaffId(staffId);
    public Task<List<LeaveReasonDto>> GetReasonsByLeaveType(int? leaveTypeId) => leaves.GetReasonsByLeaveType(leaveTypeId);
    public Task<LeaveApplicationTypeDto?> GetApplicationLeaveTypeAsync(int jobLeaveTypeId, int? applicantJobId = null, DateTime? date = null)
        => leaves.GetApplicationLeaveTypeAsync(jobLeaveTypeId, applicantJobId, date);
    public Task<LeaveHandoverRulesDto> GetHandoverRulesAsync(int jobLeaveTypeId, DateTime fromDate, int? applicantJobId = null)
        => leaves.GetHandoverRulesAsync(jobLeaveTypeId, fromDate, applicantJobId);
    public Task<List<LeaveApplicantJob>> GetLeaveApplicantJobsAsync(int staffId) => leaves.GetLeaveApplicantJobsAsync(staffId);
    public async Task<List<JobLeaveTypeDto>> GetApplicantLeaveBalancesAsync(int applicantJobId)
        => (await leaves.GetApplicantLeaveBalancesAsync(applicantJobId)).Select(ContractDtoMappings.ToDto).ToList();
    public async Task<List<JobLeaveTypeDto>> GetMyLeaveBalancesAsync(int? leaveTypeId = 0)
        => (await leaves.GetMyLeaveBalancesAsync(leaveTypeId)).Select(ContractDtoMappings.ToDto).ToList();
    public async Task<List<JobLeaveTypeDto>> GetJobLeaveBalancesAsync(int jobId, int? leaveTypeId = 0)
        => (await leaves.GetJobLeaveBalancesAsync(jobId, leaveTypeId)).Select(ContractDtoMappings.ToDto).ToList();
    public async Task<List<LeaveSummary>> GetLeaveRequestsAsync(int jobId)
        => (await leaves.GetLeaveRequestsAsync(jobId)).Select(ContractDtoMappings.ToDto).ToList();
    public async Task<List<JobLeaveTypeDto>> GetJobLeaveTypesAsync()
        => (await leaves.GetJobLeaveTypesAsync()).Select(ContractDtoMappings.ToDto).ToList();
    public async Task<List<LeaveSummary>> GetMyLeaveRequestsAsync(int? statusId = null, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null)
        => (await leaves.GetMyLeaveRequestsAsync(statusId, fromDate, toDate, limit)).Select(ContractDtoMappings.ToDto).ToList();
    public Task<ServiceResult> SubmitLeave(LeaveApplicationDto model, int? applicantJobId = null) => leaves.SubmitLeave(model, applicantJobId);
    public Task<ServiceResult> CancelLeaveAsync(int leaveId) => leaves.CancelLeaveAsync(leaveId);
    public Task<List<ProcessingLeaveDto>> GetMyProcessingLeavesAsync() => leaves.GetMyProcessingLeavesAsync();
}
