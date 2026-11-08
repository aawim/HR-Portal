// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/job-work-template-assignment")]
public sealed class JobWorkTemplateAssignmentController(global::HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment.IJobWorkTemplateAssignmentService service) : HrmApiController
{
    [HttpGet("get-current")]
    public async Task<ActionResult<global::HRM.DTOs.StaffSchedule.CurrentWorkTemplateAssignmentDto?>> GetCurrent([FromQuery] global::System.Int32 jobId, [FromQuery] global::System.DateTime effectiveDate)
    {
        return Ok(await service.GetCurrentAsync(jobId, effectiveDate, HttpContext.RequestAborted));
    }

    [HttpGet("get-history")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.StaffSchedule.JobWorkTemplateAssignmentHistoryDto>>> GetHistory([FromQuery] global::System.Int32 jobId)
    {
        return Ok(await service.GetHistoryAsync(jobId, HttpContext.RequestAborted));
    }

    [HttpGet("search-templates")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkTemplateLookupDto>>> SearchTemplates([FromQuery] global::System.Int32 organisationBusinessEntityId, [FromQuery] global::System.String? searchText)
    {
        return Ok(await service.SearchTemplatesAsync(organisationBusinessEntityId, searchText, HttpContext.RequestAborted));
    }

    [HttpPost("assign")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Assign([FromBody] JobWorkTemplateAssignmentAssignRequest request)
    {
        return Ok(await service.AssignAsync(request.dto, HttpContext.RequestAborted));
    }

    public sealed class JobWorkTemplateAssignmentAssignRequest
    {
        public global::HRM.DTOs.Job.JobWorkTemplateAssignment.AssignJobWorkTemplateDto dto { get; set; } = default!;
    }

    [HttpPost("remove")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Remove([FromBody] JobWorkTemplateAssignmentRemoveRequest request)
    {
        return Ok(await service.RemoveAsync(request.dto, HttpContext.RequestAborted));
    }

    public sealed class JobWorkTemplateAssignmentRemoveRequest
    {
        public global::HRM.DTOs.Job.JobWorkTemplateAssignment.RemoveJobWorkTemplateDto dto { get; set; } = default!;
    }

}
