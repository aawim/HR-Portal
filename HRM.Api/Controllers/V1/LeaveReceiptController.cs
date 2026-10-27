// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-receipt")]
public sealed class LeaveReceiptController(global::HRM.Services.Interfaces.Leave.ILeaveReceiptService service) : HrmApiController
{
    [HttpGet("get-my-receipt")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.LeaveReceiptDto?>> GetMyReceipt([FromQuery] global::System.Int32 leaveId)
    {
        return Ok(await service.GetMyReceiptAsync(leaveId));
    }

}
