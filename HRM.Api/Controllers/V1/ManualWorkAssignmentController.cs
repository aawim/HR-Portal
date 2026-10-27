// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/manual-work-assignment")]
public sealed class ManualWorkAssignmentController(global::HRM.WorkPlanning.Abstractions.IManualWorkAssignmentService service) : HrmApiController
{
    [HttpPost("update")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.GeneratedWorkPlanResult>> Update([FromBody] ManualWorkAssignmentUpdateRequest request)
    {
        return Ok(await service.UpdateAsync(request.assignmentId, request.request, HttpContext.RequestAborted));
    }

    public sealed class ManualWorkAssignmentUpdateRequest
    {
        public global::System.Int64 assignmentId { get; set; }
        public global::HRM.DTOs.WorkPlanning.ManualWorkAssignmentRequest request { get; set; } = default!;
    }

    [HttpGet("cancel")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.GeneratedWorkPlanResult>> Cancel([FromQuery] global::System.Int64 assignmentId, [FromQuery] global::System.String reason)
    {
        return Ok(await service.CancelAsync(assignmentId, reason, HttpContext.RequestAborted));
    }

    [HttpPost("generate")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.GeneratedWorkPlanResult>> Generate([FromBody] ManualWorkAssignmentGenerateRequest request)
    {
        return Ok(await service.GenerateAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class ManualWorkAssignmentGenerateRequest
    {
        public global::HRM.DTOs.WorkPlanning.ManualWorkAssignmentRequest request { get; set; } = default!;
    }

}
