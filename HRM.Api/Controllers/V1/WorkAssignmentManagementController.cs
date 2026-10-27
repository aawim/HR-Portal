// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/work-assignment-management")]
public sealed class WorkAssignmentManagementController(global::HRM.WorkPlanning.Abstractions.IWorkAssignmentManagementService service) : HrmApiController
{
    [HttpGet("get-departments")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkAssignmentDepartmentDto>>> GetDepartments()
    {
        return Ok(await service.GetDepartmentsAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-work-plans")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkPlanLookupDto>>> GetWorkPlans()
    {
        return Ok(await service.GetWorkPlansAsync(HttpContext.RequestAborted));
    }

    [HttpPost("query/get-assignments")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkAssignmentGroupDto>>> GetAssignments([FromBody] WorkAssignmentManagementGetAssignmentsRequest request)
    {
        return Ok(await service.GetAssignmentsAsync(request.filter, HttpContext.RequestAborted));
    }

    public sealed class WorkAssignmentManagementGetAssignmentsRequest
    {
        public global::HRM.DTOs.WorkPlanning.WorkAssignmentFilterDto filter { get; set; } = default!;
    }

    [HttpGet("get-department-staff")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.EligibleAssignmentStaffDto>>> GetDepartmentStaff([FromQuery] global::System.Int32 organisationStructureId)
    {
        return Ok(await service.GetDepartmentStaffAsync(organisationStructureId, HttpContext.RequestAborted));
    }

    [HttpGet("get-assignment-employees")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkAssignmentEmployeeDto>>> GetAssignmentEmployees([FromQuery] global::System.Int32 organisationStructureId, [FromQuery] global::System.Int32? workPlanId, [FromQuery] global::System.String assignmentName, [FromQuery] global::System.DateTime scheduledStart, [FromQuery] global::System.DateTime scheduledEnd)
    {
        return Ok(await service.GetAssignmentEmployeesAsync(organisationStructureId, workPlanId, assignmentName, scheduledStart, scheduledEnd, HttpContext.RequestAborted));
    }

    [HttpPost("assign-department")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.BulkAssignmentResultDto>> AssignDepartment([FromBody] WorkAssignmentManagementAssignDepartmentRequest request)
    {
        return Ok(await service.AssignDepartmentAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class WorkAssignmentManagementAssignDepartmentRequest
    {
        public global::HRM.DTOs.WorkPlanning.BulkDepartmentAssignmentRequest request { get; set; } = default!;
    }

}
