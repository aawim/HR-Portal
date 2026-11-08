// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/scope-schedule-settings")]
public sealed class ScopeScheduleSettingsController(global::HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Contracts.ScopeScheduleSettingsDataResponse>> Load([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.LoadAsync(organisationId));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] ScopeScheduleSettingsSaveRequest request)
    {
        await service.SaveAsync(request.model, request.organisationId);
        return NoContent();
    }

    public sealed class ScopeScheduleSettingsSaveRequest
    {
        public global::HRM.DTOs.Contracts.ScopeScheduleAssignmentDto model { get; set; } = default!;
        public global::System.Int32? organisationId { get; set; } = null;
    }

    [HttpPost("set-enabled")]
    public async Task<IActionResult> SetEnabled([FromBody] ScopeScheduleSettingsSetEnabledRequest request)
    {
        await service.SetEnabledAsync(request.id, request.version, request.enabled, request.organisationId);
        return NoContent();
    }

    public sealed class ScopeScheduleSettingsSetEnabledRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 version { get; set; }
        public global::System.Boolean enabled { get; set; }
        public global::System.Int32? organisationId { get; set; } = null;
    }

    [HttpPost("end")]
    public async Task<IActionResult> End([FromBody] ScopeScheduleSettingsEndRequest request)
    {
        await service.EndAsync(request.id, request.version, request.lastDay, request.organisationId);
        return NoContent();
    }

    public sealed class ScopeScheduleSettingsEndRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 version { get; set; }
        public global::System.DateTime lastDay { get; set; }
        public global::System.Int32? organisationId { get; set; } = null;
    }

}
