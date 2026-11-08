// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/work-plan")]
public sealed class WorkPlanController(global::HRM.WorkPlanning.Abstractions.IWorkPlanService service) : HrmApiController
{
    [HttpGet("get-work-plans")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkPlanListDto>>> GetWorkPlans([FromQuery] global::System.DateTime fromDate, [FromQuery] global::System.DateTime toDate)
    {
        return Ok(await service.GetWorkPlansAsync(fromDate, toDate));
    }

    [HttpGet("get-work-plan-detail")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.WorkPlanListDto?>> GetWorkPlanDetail([FromQuery] global::System.Int32 workPlanId)
    {
        return Ok(await service.GetWorkPlanDetailAsync(workPlanId));
    }

    [HttpGet("search-employees")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkPlanEmployeeDto>>> SearchEmployees([FromQuery] global::System.String searchText)
    {
        return Ok(await service.SearchEmployeesAsync(searchText));
    }

    [HttpGet("get-active-templates")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkTemplateLookupDto>>> GetActiveTemplates()
    {
        return Ok(await service.GetActiveTemplatesAsync());
    }

    [HttpPost("generate-work-plan")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.GenerateWorkPlanResultDto>> GenerateWorkPlan([FromBody] WorkPlanGenerateWorkPlanRequest request)
    {
        return Ok(await service.GenerateWorkPlanAsync(request.request));
    }

    public sealed class WorkPlanGenerateWorkPlanRequest
    {
        public global::HRM.DTOs.WorkPlanning.GenerateWorkPlanDto request { get; set; } = default!;
    }

    [HttpGet("get-planning-providers")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.PlanningProviderLookupDto>>> GetPlanningProviders()
    {
        return Ok(await service.GetPlanningProvidersAsync());
    }

}
