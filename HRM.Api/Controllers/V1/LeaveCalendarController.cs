// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-calendar")]
public sealed class LeaveCalendarController(global::HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.LeaveCalendarEntry>>> Load([FromQuery] global::System.DateTime month, [FromQuery] global::System.Boolean includeTeam, [FromQuery] global::System.Int32? applicantJobId = null)
    {
        return Ok(await service.LoadAsync(month, includeTeam, applicantJobId));
    }

    [HttpPost("save-plan")]
    public async Task<IActionResult> SavePlan([FromBody] LeaveCalendarSavePlanRequest request)
    {
        await service.SavePlanAsync(request.from, request.to, request.title);
        return NoContent();
    }

    public sealed class LeaveCalendarSavePlanRequest
    {
        public global::System.DateTime from { get; set; }
        public global::System.DateTime to { get; set; }
        public global::System.String title { get; set; } = default!;
    }

    [HttpPost("remove-plan")]
    public async Task<IActionResult> RemovePlan([FromBody] LeaveCalendarRemovePlanRequest request)
    {
        await service.RemovePlanAsync(request.id);
        return NoContent();
    }

    public sealed class LeaveCalendarRemovePlanRequest
    {
        public global::System.Int32 id { get; set; }
    }

}
