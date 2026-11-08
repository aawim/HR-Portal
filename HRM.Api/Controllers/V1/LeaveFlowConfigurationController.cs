// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-flow-configuration")]
public sealed class LeaveFlowConfigurationController(global::HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Admin")]
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.LeaveFlowSettingsData>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [ApiPermission("Admin")]
    [HttpGet("get")]
    public async Task<ActionResult<global::HRM.DTOs.Leave.LeaveFlowConfigurationDto>> Get([FromQuery] global::System.Int32 jobType, [FromQuery] global::System.Int32 requestType)
    {
        return Ok(await service.GetAsync(jobType, requestType));
    }

    [ApiPermission("Admin")]
    [HttpPost("save")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Save([FromBody] LeaveFlowConfigurationSaveRequest request)
    {
        return Ok(await service.SaveAsync(request.model));
    }

    public sealed class LeaveFlowConfigurationSaveRequest
    {
        public global::HRM.DTOs.Leave.LeaveFlowConfigurationDto model { get; set; } = default!;
    }

}
