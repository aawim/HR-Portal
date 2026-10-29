// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/work-type")]
public sealed class WorkTypeController(global::HRM.Services.Interfaces.IWorkTypeService service) : HrmApiController
{
    [HttpGet("can-clock")]
    public async Task<ActionResult<bool>> CanClock() => Ok(await service.CanClockAsync());
    [HttpGet("get-eligible-staff")]
    public async Task<ActionResult<List<global::HRM.DTOs.WorkTypes.WorkTypeStaffOption>>> GetEligibleStaff() => Ok(await service.GetEligibleStaffAsync());
    [HttpGet("get-all")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkTypes.WorkTypeDto>>> GetAll([FromQuery] global::System.Boolean includeInactive = false)
    {
        return Ok(await service.GetAllAsync(includeInactive));
    }

    [HttpGet("get-by-id")]
    public async Task<ActionResult<global::HRM.DTOs.WorkTypes.WorkTypeDto?>> GetById([FromQuery] global::System.Int32 workTypeId)
    {
        return Ok(await service.GetByIdAsync(workTypeId));
    }

    [ApiPermission("WorkTypes.Manage")]
    [HttpPost("create")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Create([FromBody] WorkTypeCreateRequest request)
    {
        return Ok(await service.CreateAsync(request.dto));
    }

    public sealed class WorkTypeCreateRequest
    {
        public global::HRM.DTOs.WorkTypes.CreateWorkTypeDto dto { get; set; } = default!;
    }

    [ApiPermission("WorkTypes.Manage")]
    [HttpPost("update")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Update([FromBody] WorkTypeUpdateRequest request)
    {
        return Ok(await service.UpdateAsync(request.dto));
    }

    public sealed class WorkTypeUpdateRequest
    {
        public global::HRM.DTOs.WorkTypes.UpdateWorkTypeDto dto { get; set; } = default!;
    }

    [ApiPermission("WorkTypes.Manage")]
    [HttpPost("set-active")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SetActive([FromBody] WorkTypeSetActiveRequest request)
    {
        return Ok(await service.SetActiveAsync(request.workTypeId, request.isActive));
    }

    public sealed class WorkTypeSetActiveRequest
    {
        public global::System.Int32 workTypeId { get; set; }
        public global::System.Boolean isActive { get; set; }
    }

    [HttpGet("get-assignments")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkTypes.AssignedWorkTypeDto>>> GetAssignments([FromQuery] global::System.Int32? jobId = null, [FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Boolean includeInactive = false)
    {
        return Ok(await service.GetAssignmentsAsync(jobId, organisationId, includeInactive));
    }

    [HttpGet("get-assignment-by-id")]
    public async Task<ActionResult<global::HRM.DTOs.WorkTypes.AssignedWorkTypeDto?>> GetAssignmentById([FromQuery] global::System.Int32 assignedWorkTypeId)
    {
        return Ok(await service.GetAssignmentByIdAsync(assignedWorkTypeId));
    }

    [ApiPermission("WorkTypes.Manage")]
    [HttpPost("assign")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Assign([FromBody] WorkTypeAssignRequest request)
    {
        return Ok(await service.AssignAsync(request.dto));
    }

    public sealed class WorkTypeAssignRequest
    {
        public global::HRM.DTOs.WorkTypes.CreateAssignedWorkTypeDto dto { get; set; } = default!;
    }

    [ApiPermission("WorkTypes.Manage")]
    [HttpPost("update-assignment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UpdateAssignment([FromBody] WorkTypeUpdateAssignmentRequest request)
    {
        return Ok(await service.UpdateAssignmentAsync(request.dto));
    }

    public sealed class WorkTypeUpdateAssignmentRequest
    {
        public global::HRM.DTOs.WorkTypes.UpdateAssignedWorkTypeDto dto { get; set; } = default!;
    }

    [ApiPermission("WorkTypes.Manage")]
    [HttpPost("deactivate-assignment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeactivateAssignment([FromBody] WorkTypeDeactivateAssignmentRequest request)
    {
        return Ok(await service.DeactivateAssignmentAsync(request.assignedWorkTypeId));
    }

    public sealed class WorkTypeDeactivateAssignmentRequest
    {
        public global::System.Int32 assignedWorkTypeId { get; set; }
    }

}
