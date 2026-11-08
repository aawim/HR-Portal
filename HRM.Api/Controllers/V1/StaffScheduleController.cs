// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-schedule")]
public sealed class StaffScheduleController(global::HRM.WorkPlanning.Abstractions.Schedules.IStaffScheduleService service) : HrmApiController
{
    [HttpGet("get-schedule")]
    public async Task<ActionResult<global::HRM.DTOs.StaffSchedule.StaffScheduleResult>> GetSchedule([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetScheduleAsync(individualId, HttpContext.RequestAborted));
    }

}
