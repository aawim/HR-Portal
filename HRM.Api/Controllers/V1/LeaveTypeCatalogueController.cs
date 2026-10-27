// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-type-catalogue")]
public sealed class LeaveTypeCatalogueController(global::HRM.Services.Interfaces.LeaveType.ILeaveTypeCatalogueService service) : HrmApiController
{
    [HttpGet("get-request-types")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.LeaveTypes.LeaveRequestTypeOption>>> GetRequestTypes()
    {
        return Ok(await service.GetRequestTypesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-management")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.LeaveTypes.LeaveTypeCatalogueDto>>> GetManagement()
    {
        return Ok(await service.GetManagementAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-available")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.LeaveTypes.LeaveTypeCatalogueDto>>> GetAvailable()
    {
        return Ok(await service.GetAvailableAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-by-id")]
    public async Task<ActionResult<global::HRM.DTOs.LeaveTypes.LeaveTypeCatalogueDto?>> GetById([FromQuery] global::System.Int32 leaveDefinitionId)
    {
        return Ok(await service.GetByIdAsync(leaveDefinitionId, HttpContext.RequestAborted));
    }

    [HttpPost("create")]
    public async Task<ActionResult<global::HRM.DTOs.LeaveTypes.ServiceResult<global::System.Int32>>> Create([FromBody] LeaveTypeCatalogueCreateRequest request)
    {
        return Ok(await service.CreateAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class LeaveTypeCatalogueCreateRequest
    {
        public global::HRM.DTOs.LeaveTypes.LeaveTypeCatalogueSaveRequest request { get; set; } = default!;
    }

    [HttpPost("update")]
    public async Task<ActionResult<global::HRM.DTOs.LeaveTypes.ServiceResult<global::System.Int32>>> Update([FromBody] LeaveTypeCatalogueUpdateRequest request)
    {
        return Ok(await service.UpdateAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class LeaveTypeCatalogueUpdateRequest
    {
        public global::HRM.DTOs.LeaveTypes.LeaveTypeCatalogueSaveRequest request { get; set; } = default!;
    }

}
