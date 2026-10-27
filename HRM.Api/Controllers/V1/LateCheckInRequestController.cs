// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/late-check-in-request")]
public sealed class LateCheckInRequestController(global::HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations service) : HrmApiController
{
    [HttpPost("submit-for-date")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitForDate([FromBody] LateCheckInRequestSubmitForDateRequest request)
    {
        return Ok(await service.SubmitForDateAsync(request.date, request.time, request.reason, request.kind));
    }

    public sealed class LateCheckInRequestSubmitForDateRequest
    {
        public global::System.DateTime date { get; set; }
        public global::System.TimeOnly time { get; set; }
        public global::System.String reason { get; set; } = default!;
        public global::System.String kind { get; set; } = default!;
    }

    [HttpGet("get-candidates")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.StaffScheduleAttendanceDto>>> GetCandidates([FromQuery] global::System.DateTime from, [FromQuery] global::System.DateTime to, [FromQuery] global::System.String kind = "LateCheckIn")
    {
        return Ok(await service.GetCandidatesAsync(from, to, kind));
    }

    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] LateCheckInRequestSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.assignment, request.requestedTime, request.reason, request.kind));
    }

    public sealed class LateCheckInRequestSubmitRequest
    {
        public global::System.Int64 assignment { get; set; }
        public global::System.DateTime requestedTime { get; set; }
        public global::System.String reason { get; set; } = default!;
        public global::System.String kind { get; set; } = "LateCheckIn";
    }

    [HttpGet("load")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.LateCheckInRow>>> Load([FromQuery] global::System.DateTime from, [FromQuery] global::System.DateTime to, [FromQuery] global::System.String? kind = null, [FromQuery] global::System.Boolean pendingOnly = false)
    {
        return Ok(await service.LoadAsync(from, to, kind, pendingOnly));
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] LateCheckInRequestActRequest request)
    {
        return Ok(await service.ActAsync(request.id, request.expectedState, request.action, request.comment));
    }

    public sealed class LateCheckInRequestActRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

}
