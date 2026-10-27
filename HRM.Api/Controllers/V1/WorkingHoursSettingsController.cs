// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/working-hours-settings")]
public sealed class WorkingHoursSettingsController(global::HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Contracts.WorkingHoursSettingsDataResponse>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpPost("save-rule")]
    public async Task<IActionResult> SaveRule([FromBody] WorkingHoursSettingsSaveRuleRequest request)
    {
        await service.SaveRuleAsync(request.model);
        return NoContent();
    }

    public sealed class WorkingHoursSettingsSaveRuleRequest
    {
        public global::HRM.DTOs.Contracts.JobTypeWorkingHourRuleDto model { get; set; } = default!;
    }

    [HttpPost("save-requirement")]
    public async Task<IActionResult> SaveRequirement([FromBody] WorkingHoursSettingsSaveRequirementRequest request)
    {
        await service.SaveRequirementAsync(request.model);
        return NoContent();
    }

    public sealed class WorkingHoursSettingsSaveRequirementRequest
    {
        public global::HRM.DTOs.Contracts.OrganisationWorkingHourRequirementDto model { get; set; } = default!;
    }

    [HttpPost("save-amendment")]
    public async Task<IActionResult> SaveAmendment([FromBody] WorkingHoursSettingsSaveAmendmentRequest request)
    {
        await service.SaveAmendmentAsync(request.model);
        return NoContent();
    }

    public sealed class WorkingHoursSettingsSaveAmendmentRequest
    {
        public global::HRM.DTOs.Contracts.OrganisationScheduleAmendmentDto model { get; set; } = default!;
    }

}
