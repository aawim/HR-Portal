// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/request-notification")]
public sealed class RequestNotificationController(global::HRM.Services.Contracts.RequestNotificationServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-inbox")]
    public async Task<ActionResult<global::HRM.Services.RequestNotificationInbox>> GetInbox()
    {
        return Ok(await service.GetInboxAsync(HttpContext.RequestAborted));
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead([FromBody] RequestNotificationMarkReadRequest request)
    {
        await service.MarkReadAsync(request.notificationId, HttpContext.RequestAborted);
        return NoContent();
    }

    public sealed class RequestNotificationMarkReadRequest
    {
        public global::System.Int64? notificationId { get; set; } = null;
    }

}
