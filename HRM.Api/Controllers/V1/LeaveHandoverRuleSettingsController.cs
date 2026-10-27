// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-handover-rule-settings")]
public sealed class LeaveHandoverRuleSettingsController(global::HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.HandoverRuleSettingsData>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpPost("staff")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.HandoverOption>>> Staff([FromBody] LeaveHandoverRuleSettingsStaffRequest request)
    {
        return Ok(await service.StaffAsync(request.search));
    }

    public sealed class LeaveHandoverRuleSettingsStaffRequest
    {
        public global::System.String search { get; set; } = default!;
    }

    [HttpGet("preview")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.EffectiveHandoverRule>> Preview([FromQuery] global::System.Int32 jobId, [FromQuery] global::System.Int32 typeId, [FromQuery] global::System.DateTime date)
    {
        return Ok(await service.PreviewAsync(jobId, typeId, date));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] LeaveHandoverRuleSettingsSaveRequest request)
    {
        await service.SaveAsync(request.m);
        return NoContent();
    }

    public sealed class LeaveHandoverRuleSettingsSaveRequest
    {
        public global::HRM.DTOs.Leave.HandoverRuleEdit m { get; set; } = default!;
    }

}
