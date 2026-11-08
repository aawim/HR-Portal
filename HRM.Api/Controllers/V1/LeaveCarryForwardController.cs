// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-carry-forward")]
public sealed class LeaveCarryForwardController(global::HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("pending")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.Leave.CarryForwardReview>>> Pending()
    {
        return Ok(await service.PendingAsync());
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] LeaveCarryForwardProcessRequest request)
    {
        await service.ProcessAsync(request.preview, request.days, request.reason);
        return NoContent();
    }

    public sealed class LeaveCarryForwardProcessRequest
    {
        public global::HRM.Services.Leave.CarryForwardReview preview { get; set; } = default!;
        public global::System.Decimal days { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpGet("history")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.LeaveCarryForwardDto>>> History()
    {
        return Ok(await service.HistoryAsync());
    }

}
