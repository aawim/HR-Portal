// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-request-review")]
public sealed class AttendanceRequestReviewController(global::HRM.Application.Contracts.Pages.IAttendanceRequestReview service) : HrmApiController
{
    [HttpGet("get")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.AttendanceReview>> Get([FromQuery] global::System.Int32 requestId)
    {
        return Ok(await service.GetAsync(requestId));
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] AttendanceRequestReviewActRequest request)
    {
        return Ok(await service.ActAsync(request.requestId, request.expectedState, request.action, request.comment));
    }

    public sealed class AttendanceRequestReviewActRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

}
