// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-request")]
public sealed class AttendanceRequestController(global::HRM.Services.Interfaces.IAttendanceRequestService service) : HrmApiController
{
    [HttpGet("get-date-info")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceRequestDateInfoDto>> GetDateInfo([FromQuery] global::System.DateOnly date, [FromQuery] global::System.Int32? originalLogId = null)
    {
        return Ok(await service.GetDateInfoAsync(date, originalLogId));
    }

    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] AttendanceRequestSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.model));
    }

    public sealed class AttendanceRequestSubmitRequest
    {
        public global::HRM.DTOs.Attendance.AttendanceRequestApplicationDto model { get; set; } = default!;
    }

}
