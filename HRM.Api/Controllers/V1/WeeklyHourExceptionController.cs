// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/weekly-hour-exception")]
public sealed class WeeklyHourExceptionController(global::HRM.Services.Contracts.WeeklyHourExceptionServiceContracts.IOperations service) : HrmApiController
{
    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] WeeklyHourExceptionSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.day, request.minutes, request.reason));
    }

    public sealed class WeeklyHourExceptionSubmitRequest
    {
        public global::System.DateTime day { get; set; }
        public global::System.Int32 minutes { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpGet("load")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.WeeklyHourExceptionRow>>> Load([FromQuery] global::System.Boolean pendingOnly)
    {
        return Ok(await service.LoadAsync(pendingOnly));
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] WeeklyHourExceptionActRequest request)
    {
        return Ok(await service.ActAsync(request.id, request.expectedState, request.action, request.comment));
    }

    public sealed class WeeklyHourExceptionActRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

}
