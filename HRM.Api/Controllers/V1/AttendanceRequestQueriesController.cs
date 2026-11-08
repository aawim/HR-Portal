// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-request-queries")]
public sealed class AttendanceRequestQueriesController(global::HRM.Application.Contracts.Pages.IAttendanceRequestQueries service) : HrmApiController
{
    [HttpPost("query/get-mine")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.AttendanceRequestPage>> GetMine([FromBody] AttendanceRequestQueriesGetMineRequest request)
    {
        return Ok(await service.GetMineAsync(request.filter, HttpContext.RequestAborted));
    }

    public sealed class AttendanceRequestQueriesGetMineRequest
    {
        public global::HRM.Application.Contracts.Pages.AttendanceRequestFilter filter { get; set; } = default!;
    }

    [HttpPost("query/get-received")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.AttendanceRequestPage>> GetReceived([FromBody] AttendanceRequestQueriesGetReceivedRequest request)
    {
        return Ok(await service.GetReceivedAsync(request.filter, HttpContext.RequestAborted));
    }

    public sealed class AttendanceRequestQueriesGetReceivedRequest
    {
        public global::HRM.Application.Contracts.Pages.AttendanceRequestFilter filter { get; set; } = default!;
    }

}
