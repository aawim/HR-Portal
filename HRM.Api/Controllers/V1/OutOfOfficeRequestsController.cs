// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/out-of-office-requests")]
public sealed class OutOfOfficeRequestsController(global::HRM.Application.Contracts.Pages.IOutOfOfficeRequests service) : HrmApiController
{
    [HttpGet("get-options")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.OutOfOfficeOptions>> GetOptions()
    {
        return Ok(await service.GetOptionsAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-team-members")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.OutOfOfficeTeamMembers>> GetTeamMembers([FromQuery] global::System.Int32 teamId)
    {
        return Ok(await service.GetTeamMembersAsync(teamId, HttpContext.RequestAborted));
    }

    [HttpPost("query/get-mine")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.OutOfOfficeRequestRow>>> GetMine([FromBody] OutOfOfficeRequestsGetMineRequest request)
    {
        return Ok(await service.GetMineAsync(request.filter, HttpContext.RequestAborted));
    }

    public sealed class OutOfOfficeRequestsGetMineRequest
    {
        public global::HRM.Application.Contracts.Pages.OutOfOfficeFilter filter { get; set; } = default!;
    }

    [HttpPost("query/get-received")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.OutOfOfficeRequestRow>>> GetReceived([FromBody] OutOfOfficeRequestsGetReceivedRequest request)
    {
        return Ok(await service.GetReceivedAsync(request.filter, HttpContext.RequestAborted));
    }

    public sealed class OutOfOfficeRequestsGetReceivedRequest
    {
        public global::HRM.Application.Contracts.Pages.OutOfOfficeFilter filter { get; set; } = default!;
    }

    [HttpGet("get-editable")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.OutOfOfficeApplication?>> GetEditable([FromQuery] global::System.Int32 requestId)
    {
        return Ok(await service.GetEditableAsync(requestId, HttpContext.RequestAborted));
    }

    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] OutOfOfficeRequestsSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.application, HttpContext.RequestAborted));
    }

    public sealed class OutOfOfficeRequestsSubmitRequest
    {
        public global::HRM.Application.Contracts.Pages.OutOfOfficeApplication application { get; set; } = default!;
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] OutOfOfficeRequestsActRequest request)
    {
        return Ok(await service.ActAsync(request.requestId, request.expectedState, request.action, request.comment, HttpContext.RequestAborted));
    }

    public sealed class OutOfOfficeRequestsActRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

}
