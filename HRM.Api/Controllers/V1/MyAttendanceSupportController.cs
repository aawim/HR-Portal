// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/my-attendance-support")]
public sealed class MyAttendanceSupportController(global::HRM.Services.Interfaces.IMyAttendanceSupportService service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.MyAttendanceSupport>> Load([FromQuery] global::System.DateTime from, [FromQuery] global::System.DateTime to)
    {
        return Ok(await service.LoadAsync(from, to, HttpContext.RequestAborted));
    }

}
