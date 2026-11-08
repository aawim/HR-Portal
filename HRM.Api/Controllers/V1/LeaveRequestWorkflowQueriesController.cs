// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-request-workflow-queries")]
public sealed class LeaveRequestWorkflowQueriesController(global::HRM.Application.Contracts.Pages.ILeaveRequestWorkflowQueries service) : HrmApiController
{
    [HttpGet("get-states")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.RequestStateOption>>> GetStates()
    {
        return Ok(await service.GetStatesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-leave-states")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.DirectoryOption>>> GetLeaveStates()
    {
        return Ok(await service.GetLeaveStatesAsync(HttpContext.RequestAborted));
    }

    [HttpPost("query/get-mine")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.LeaveRequestWorkflowSummary>>> GetMine([FromBody] LeaveRequestWorkflowQueriesGetMineRequest request)
    {
        return Ok(await service.GetMineAsync(request.leaveIds, HttpContext.RequestAborted));
    }

    public sealed class LeaveRequestWorkflowQueriesGetMineRequest
    {
        public global::System.Collections.Generic.IReadOnlyList<global::System.Int32> leaveIds { get; set; } = default!;
    }

}
