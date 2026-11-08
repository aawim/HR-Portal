// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/manual-work-assignment-lookup")]
public sealed class ManualWorkAssignmentLookupController(global::HRM.WorkPlanning.Abstractions.IManualWorkAssignmentLookupService service) : HrmApiController
{
    [HttpGet("get-templates-for-job")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.AssignedWorkTemplateDto>>> GetTemplatesForJob([FromQuery] global::System.Int32 jobId, [FromQuery] global::System.DateTime workDate)
    {
        return Ok(await service.GetTemplatesForJobAsync(jobId, workDate, HttpContext.RequestAborted));
    }

    [HttpGet("get-planning-providers")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.PlanningProviderLookupDto>>> GetPlanningProviders([FromQuery] global::System.Int32 organisationBusinessEntityId)
    {
        return Ok(await service.GetPlanningProvidersAsync(organisationBusinessEntityId, HttpContext.RequestAborted));
    }

    [HttpGet("search-active-jobs")]
    public async Task<ActionResult<global::System.Collections.Generic.IReadOnlyList<global::HRM.DTOs.WorkPlanning.ManualAssignmentJobDto>>> SearchActiveJobs([FromQuery] global::System.String? searchText, [FromQuery] global::System.Int32? organisationId)
    {
        return Ok(await service.SearchActiveJobsAsync(searchText, organisationId, HttpContext.RequestAborted));
    }

    [HttpGet("get-assignment-preview")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.WorkAssignmentPreviewDto?>> GetAssignmentPreview([FromQuery] global::System.Int32 jobId, [FromQuery] global::System.Int32 workTemplateId, [FromQuery] global::System.DateTime workDate)
    {
        return Ok(await service.GetAssignmentPreviewAsync(jobId, workTemplateId, workDate, HttpContext.RequestAborted));
    }

    [HttpGet("get-available-templates")]
    public async Task<ActionResult<global::System.Collections.Generic.IReadOnlyList<global::HRM.DTOs.WorkPlanning.AssignedWorkTemplateDto>>> GetAvailableTemplates([FromQuery] global::System.Int32 organisationId, [FromQuery] global::System.DateTime workDate)
    {
        return Ok(await service.GetAvailableTemplatesAsync(organisationId, workDate, HttpContext.RequestAborted));
    }

}
