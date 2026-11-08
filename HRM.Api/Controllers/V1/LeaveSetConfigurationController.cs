// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-set-configuration")]
public sealed class LeaveSetConfigurationController(global::HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load-contents")]
    public async Task<ActionResult<global::HRM.Services.LeaveSetContentsData>> LoadContents([FromQuery] global::System.Int32 id)
    {
        return Ok(await service.LoadContentsAsync(id));
    }

    [HttpPost("save-contents")]
    public async Task<IActionResult> SaveContents([FromBody] LeaveSetConfigurationSaveContentsRequest request)
    {
        await service.SaveContentsAsync(request.id, request.typeIds, request.expectedAudit);
        return NoContent();
    }

    public sealed class LeaveSetConfigurationSaveContentsRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Collections.Generic.IReadOnlyCollection<global::System.Int32> typeIds { get; set; } = default!;
        public global::System.Int32 expectedAudit { get; set; }
    }

    [HttpGet("load-job-type")]
    public async Task<ActionResult<global::HRM.Services.JobTypeLeaveSetData>> LoadJobType([FromQuery] global::System.Int32 jobTypeId)
    {
        return Ok(await service.LoadJobTypeAsync(jobTypeId));
    }

    [HttpPost("save-job-type")]
    public async Task<IActionResult> SaveJobType([FromBody] LeaveSetConfigurationSaveJobTypeRequest request)
    {
        await service.SaveJobTypeAsync(request.jobTypeId, request.setIds, request.expectedVersion);
        return NoContent();
    }

    public sealed class LeaveSetConfigurationSaveJobTypeRequest
    {
        public global::System.Int32 jobTypeId { get; set; }
        public global::System.Collections.Generic.IReadOnlyCollection<global::System.Int32> setIds { get; set; } = default!;
        public global::System.String expectedVersion { get; set; } = default!;
    }

    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Contracts.LeaveSetConfigurationDataResponse>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] LeaveSetConfigurationSaveRequest request)
    {
        await service.SaveAsync(request.model);
        return NoContent();
    }

    public sealed class LeaveSetConfigurationSaveRequest
    {
        public global::HRM.DTOs.Contracts.LeaveSetDto model { get; set; } = default!;
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] LeaveSetConfigurationDeleteRequest request)
    {
        await service.DeleteAsync(request.model);
        return NoContent();
    }

    public sealed class LeaveSetConfigurationDeleteRequest
    {
        public global::HRM.DTOs.Contracts.LeaveSetDto model { get; set; } = default!;
    }

}
