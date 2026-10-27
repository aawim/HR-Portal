// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/job-leave-type")]
public sealed class JobLeaveTypeController(global::HRM.Services.Interfaces.JobLeaveTypes.IJobLeaveTypeService service) : HrmApiController
{
    [HttpPost("update")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Update([FromBody] JobLeaveTypeUpdateRequest request)
    {
        return Ok(await service.UpdateAsync(request.dto));
    }

    public sealed class JobLeaveTypeUpdateRequest
    {
        public global::HRM.DTOs.Leave.JobLeaveTypeEditDto dto { get; set; } = default!;
    }

    [HttpGet("get-job-leave-type-default")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetJobLeaveTypeDefault()
    {
        return Ok(await service.GetJobLeaveType());
    }

    [HttpGet("get-job-leave-type-by-job-id")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetJobLeaveTypeByJobId([FromQuery] global::System.Int32 JobId)
    {
        return Ok(await service.GetJobLeaveType(JobId));
    }

    [HttpGet("get-entitlements")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetEntitlements([FromQuery] global::System.Int32 jobId)
    {
        return Ok(await service.GetEntitlementsAsync(jobId));
    }

    [HttpPost("assign")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Assign([FromBody] JobLeaveTypeAssignRequest request)
    {
        return Ok(await service.AssignAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class JobLeaveTypeAssignRequest
    {
        public global::HRM.DTOs.LeaveTypes.AssignLeaveTypeCatalogueDto request { get; set; } = default!;
    }

}
