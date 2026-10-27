// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-accrual-settings")]
public sealed class LeaveAccrualSettingsController(global::HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Admin")]
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Contracts.LeaveAccrualSettingsDataResponse>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [ApiPermission("Admin")]
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] LeaveAccrualSettingsSaveRequest request)
    {
        await service.SaveAsync(request.model);
        return NoContent();
    }

    public sealed class LeaveAccrualSettingsSaveRequest
    {
        public global::HRM.DTOs.Contracts.JobTypeLeaveAccrualRuleDto model { get; set; } = default!;
    }

    [ApiPermission("Admin")]
    [HttpGet("preview")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.Leave.AccrualPreview>>> Preview([FromQuery] global::System.Int32 ruleId)
    {
        return Ok(await service.PreviewAsync(ruleId));
    }

    [ApiPermission("Admin")]
    [HttpPost("run")]
    public async Task<ActionResult<global::System.Int32>> Run([FromBody] LeaveAccrualSettingsRunRequest request)
    {
        return Ok(await service.RunAsync(request.ruleId));
    }

    public sealed class LeaveAccrualSettingsRunRequest
    {
        public global::System.Int32 ruleId { get; set; }
    }

}
