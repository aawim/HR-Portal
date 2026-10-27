// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-change")]
public sealed class LeaveChangeController(global::HRM.Services.Interfaces.Leave.ILeaveChangeService service) : HrmApiController
{
    [HttpGet("get-my-applications")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.MyLeaveApplicationDto>>> GetMyApplications()
    {
        return Ok(await service.GetMyApplicationsAsync());
    }

    [HttpGet("get-my-application-details")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.MyLeaveApplicationDto>> GetMyApplicationDetails([FromQuery] global::System.Int32 requestId)
    {
        return Ok(await service.GetMyApplicationDetailsAsync(requestId));
    }

    [HttpGet("get-returned-application")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.ReturnedLeaveApplicationDto>> GetReturnedApplication([FromQuery] global::System.Int32 requestId)
    {
        return Ok(await service.GetReturnedApplicationAsync(requestId));
    }

    [HttpPost("resubmit-application")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ResubmitApplication([FromBody] LeaveChangeResubmitApplicationRequest request)
    {
        return Ok(await service.ResubmitApplicationAsync(request.model));
    }

    public sealed class LeaveChangeResubmitApplicationRequest
    {
        public global::HRM.DTOs.Leave.ReturnedLeaveApplicationDto model { get; set; } = default!;
    }

    [HttpGet("get-my-leaves")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveChangeLeaveDto>>> GetMyLeaves()
    {
        return Ok(await service.GetMyLeavesAsync());
    }

    [HttpGet("get-my-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveChangeRequestDto>>> GetMyRequests()
    {
        return Ok(await service.GetMyRequestsAsync());
    }

    [HttpGet("get-assigned-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveChangeRequestDto>>> GetAssignedRequests()
    {
        return Ok(await service.GetAssignedRequestsAsync());
    }

    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] LeaveChangeSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.model));
    }

    public sealed class LeaveChangeSubmitRequest
    {
        public global::HRM.DTOs.Leave.LeaveChangeApplicationDto model { get; set; } = default!;
    }

    [HttpPost("execute-action")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ExecuteAction([FromBody] LeaveChangeExecuteActionRequest request)
    {
        return Ok(await service.ExecuteActionAsync(request.requestId, request.action));
    }

    public sealed class LeaveChangeExecuteActionRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::HRM.DTOs.Leave.RequestActionSubmissionDto action { get; set; } = default!;
    }

    [HttpGet("get-pending-leave-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.PendingLeaveRequestDto>>> GetPendingLeaveRequests()
    {
        return Ok(await service.GetPendingLeaveRequestsAsync());
    }

    [HttpPost("execute-leave-action")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ExecuteLeaveAction([FromBody] LeaveChangeExecuteLeaveActionRequest request)
    {
        return Ok(await service.ExecuteLeaveActionAsync(request.requestId, request.action));
    }

    public sealed class LeaveChangeExecuteLeaveActionRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::HRM.DTOs.Leave.RequestActionSubmissionDto action { get; set; } = default!;
    }

}
