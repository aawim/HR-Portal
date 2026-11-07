// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-applications")]
public sealed class LeaveApplicationsController(global::HRM.Application.Contracts.Pages.ILeaveApplications service) : HrmApiController
{
    [HttpGet("get-job-id-by-staff-id")]
    public async Task<ActionResult<global::System.Int32>> GetJobIdByStaffId([FromQuery] global::System.Int32 staffId)
    {
        return Ok(await service.GetJobIdByStaffId(staffId));
    }

    [HttpGet("get-reasons-by-leave-type")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveReasonDto>>> GetReasonsByLeaveType([FromQuery] global::System.Int32? leaveTypeId)
    {
        return Ok(await service.GetReasonsByLeaveType(leaveTypeId));
    }

    [HttpGet("get-application-leave-type")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.LeaveApplicationTypeDto?>> GetApplicationLeaveType([FromQuery] global::System.Int32 jobLeaveTypeId, [FromQuery] global::System.Int32? applicantJobId = null, [FromQuery] global::System.DateTime? date = null)
    {
        return Ok(await service.GetApplicationLeaveTypeAsync(jobLeaveTypeId, applicantJobId, date));
    }

    [HttpGet("get-handover-rules")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.LeaveHandoverRulesDto>> GetHandoverRules([FromQuery] global::System.Int32 jobLeaveTypeId, [FromQuery] global::System.DateTime fromDate, [FromQuery] global::System.Int32? applicantJobId = null)
    {
        return Ok(await service.GetHandoverRulesAsync(jobLeaveTypeId, fromDate, applicantJobId));
    }

    [HttpGet("get-leave-applicant-jobs")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveApplicantJob>>> GetLeaveApplicantJobs([FromQuery] global::System.Int32 staffId)
    {
        return Ok(await service.GetLeaveApplicantJobsAsync(staffId));
    }

    [HttpGet("get-applicant-leave-balances")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetApplicantLeaveBalances([FromQuery] global::System.Int32 applicantJobId)
    {
        return Ok(await service.GetApplicantLeaveBalancesAsync(applicantJobId));
    }

    [HttpGet("get-my-leave-balances")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetMyLeaveBalances([FromQuery] global::System.Int32? leaveTypeId = 0)
    {
        return Ok(await service.GetMyLeaveBalancesAsync(leaveTypeId));
    }

    [HttpGet("get-job-leave-balances")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetJobLeaveBalances([FromQuery] global::System.Int32 jobId, [FromQuery] global::System.Int32? leaveTypeId = 0)
    {
        return Ok(await service.GetJobLeaveBalancesAsync(jobId, leaveTypeId));
    }

    [HttpGet("get-leave-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.LeafDto>>> GetLeaveRequests([FromQuery] global::System.Int32 jobId)
    {
        return Ok(await service.GetLeaveRequestsAsync(jobId));
    }

    [HttpGet("get-job-leave-types")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetJobLeaveTypes()
    {
        return Ok(await service.GetJobLeaveTypesAsync());
    }

    [HttpGet("get-my-leave-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.LeafDto>>> GetMyLeaveRequests([FromQuery] global::System.Int32? statusId = null, [FromQuery] global::System.DateTime? fromDate = null, [FromQuery] global::System.DateTime? toDate = null, [FromQuery] global::System.Int32? limit = null)
    {
        return Ok(await service.GetMyLeaveRequestsAsync(statusId, fromDate, toDate, limit));
    }

    [HttpPost("submit-leave")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitLeave([FromBody] LeaveApplicationsSubmitLeaveRequest request)
    {
        return Ok(await service.SubmitLeave(request.model, request.applicantJobId));
    }

    public sealed class LeaveApplicationsSubmitLeaveRequest
    {
        public global::HRM.DTOs.Leave.LeaveApplicationDto model { get; set; } = default!;
        public global::System.Int32? applicantJobId { get; set; } = null;
    }

    [HttpGet("cancel-leave")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CancelLeave([FromQuery] global::System.Int32 leaveId)
    {
        return Ok(await service.CancelLeaveAsync(leaveId));
    }

    [HttpGet("get-my-processing-leaves")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.ProcessingLeaveDto>>> GetMyProcessingLeaves()
    {
        return Ok(await service.GetMyProcessingLeavesAsync());
    }

}
