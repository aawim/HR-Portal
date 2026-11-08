// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-configuration-settings")]
public sealed class LeaveConfigurationSettingsController(global::HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Admin")]
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Contracts.LeaveConfigurationDataResponse>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [ApiPermission("Admin")]
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] LeaveConfigurationSettingsSaveRequest request)
    {
        await service.SaveAsync(request.model);
        return NoContent();
    }

    public sealed class LeaveConfigurationSettingsSaveRequest
    {
        public global::HRM.DTOs.Contracts.LeaveConfigurableValueDto model { get; set; } = default!;
    }

    [ApiPermission("Admin")]
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] LeaveConfigurationSettingsDeleteRequest request)
    {
        await service.DeleteAsync(request.id);
        return NoContent();
    }

    public sealed class LeaveConfigurationSettingsDeleteRequest
    {
        public global::System.Int32 id { get; set; }
    }

}
