// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/supervisor-change-request")]
public sealed class SupervisorChangeRequestController(global::HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations service) : HrmApiController
{
    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] SupervisorChangeRequestSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.organisationId, request.kind, request.target, request.person, request.assign, request.reason));
    }

    public sealed class SupervisorChangeRequestSubmitRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.String kind { get; set; } = default!;
        public global::System.Int32 target { get; set; }
        public global::System.Int32 person { get; set; }
        public global::System.Boolean assign { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpGet("load")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.SupervisorRequestRow>>> Load([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Boolean pendingOnly = false)
    {
        return Ok(await service.LoadAsync(organisationId, pendingOnly));
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] SupervisorChangeRequestActRequest request)
    {
        return Ok(await service.ActAsync(request.id, request.expectedState, request.action, request.comment, request.organisationId));
    }

    public sealed class SupervisorChangeRequestActRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
        public global::System.Int32? organisationId { get; set; } = null;
    }

}
