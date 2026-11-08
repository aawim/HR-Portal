// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/job-type-overtime-settings")]
public sealed class JobTypeOvertimeSettingsController(global::HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto>> Load([FromQuery] global::System.Int32 jobTypeId)
    {
        return Ok(await service.LoadAsync(jobTypeId));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] JobTypeOvertimeSettingsSaveRequest request)
    {
        await service.SaveAsync(request.model);
        return NoContent();
    }

    public sealed class JobTypeOvertimeSettingsSaveRequest
    {
        public global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto model { get; set; } = default!;
    }

}
