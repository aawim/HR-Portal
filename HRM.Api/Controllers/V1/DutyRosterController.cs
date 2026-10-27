// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/duty-roster")]
public sealed class DutyRosterController(global::HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-partners")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.DutyPartner>>> GetPartners()
    {
        return Ok(await service.GetPartnersAsync());
    }

    [HttpGet("get-roster")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.DutyRosterRow>>> GetRoster([FromQuery] global::System.DateTime date)
    {
        return Ok(await service.GetRosterAsync(date));
    }

    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] DutyRosterSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.model));
    }

    public sealed class DutyRosterSubmitRequest
    {
        public global::HRM.DTOs.WorkPlanning.DutyChangeApplication model { get; set; } = default!;
    }

    [HttpGet("get-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.DutyRequestRow>>> GetRequests()
    {
        return Ok(await service.GetRequestsAsync());
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] DutyRosterActRequest request)
    {
        return Ok(await service.ActAsync(request.id, request.expectedState, request.action, request.comment));
    }

    public sealed class DutyRosterActRequest
    {
        public global::System.Int64 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; } = null;
    }

}
