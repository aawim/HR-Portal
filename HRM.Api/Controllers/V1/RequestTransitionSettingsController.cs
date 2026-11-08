// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/request-transition-settings")]
public sealed class RequestTransitionSettingsController(global::HRM.Services.Contracts.RequestTransitionSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.RequestTransitionSettingsDto>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpGet("get")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.AllowedRequestTransitionDto>>> Get([FromQuery] global::System.Int32 requestType)
    {
        return Ok(await service.GetAsync(requestType));
    }

    [HttpPost("set")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Set([FromBody] RequestTransitionSettingsSetRequest request)
    {
        return Ok(await service.SetAsync(request.requestType, request.from, request.to, request.allowed));
    }

    public sealed class RequestTransitionSettingsSetRequest
    {
        public global::System.Int32 requestType { get; set; }
        public global::System.Int32 from { get; set; }
        public global::System.Int32 to { get; set; }
        public global::System.Boolean allowed { get; set; }
    }

}
