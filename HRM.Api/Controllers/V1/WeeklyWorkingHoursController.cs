// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/weekly-working-hours")]
public sealed class WeeklyWorkingHoursController(global::HRM.Services.Contracts.WeeklyWorkingHoursServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.WeeklyWorkedHoursDto>> Load([FromQuery] global::System.Int32 person, [FromQuery] global::System.DateTime date)
    {
        return Ok(await service.LoadAsync(person, date));
    }

}
