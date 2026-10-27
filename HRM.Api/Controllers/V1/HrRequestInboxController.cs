// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/hr-request-inbox")]
public sealed class HrRequestInboxController(global::HRM.Services.Contracts.HrRequestInboxServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Services.HrInboxData>> Load([FromQuery] global::System.Int32? typeId = null, [FromQuery] global::System.Int32 page = 1, [FromQuery] global::System.String? search = null, [FromQuery] global::System.String status = "all", [FromQuery] global::System.DateTime? from = null, [FromQuery] global::System.DateTime? to = null)
    {
        return Ok(await service.LoadAsync(typeId, page, search, status, from, to, HttpContext.RequestAborted));
    }

}
