// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-handover")]
public sealed class LeaveHandoverController(global::HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations service) : HrmApiController
{
    [HttpPost("inbox")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveHandoverInboxItem>>> Inbox([FromBody] LeaveHandoverInboxRequest request)
    {
        return Ok(await service.InboxAsync(request.leaveId));
    }

    public sealed class LeaveHandoverInboxRequest
    {
        public global::System.Int32? leaveId { get; set; } = null;
    }

    [HttpGet("get-assigned-inbox")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveHandoverInboxItem>>> GetAssignedInbox()
    {
        return Ok(await service.GetAssignedInboxAsync());
    }

    [HttpPost("decide")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Decide([FromBody] LeaveHandoverDecideRequest request)
    {
        return Ok(await service.DecideAsync(request.leaveId, request.revision, request.taskId, request.approve, request.comments));
    }

    public sealed class LeaveHandoverDecideRequest
    {
        public global::System.Int32 leaveId { get; set; }
        public global::System.Int32 revision { get; set; }
        public global::System.Int32? taskId { get; set; }
        public global::System.Boolean approve { get; set; }
        public global::System.String? comments { get; set; }
    }

    [HttpPost("revise")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Revise([FromBody] LeaveHandoverReviseRequest request)
    {
        return Ok(await service.ReviseAsync(request.leaveId, request.revision, request.tasks));
    }

    public sealed class LeaveHandoverReviseRequest
    {
        public global::System.Int32 leaveId { get; set; }
        public global::System.Int32 revision { get; set; }
        public global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveHandoverTaskInput> tasks { get; set; } = default!;
    }

}
