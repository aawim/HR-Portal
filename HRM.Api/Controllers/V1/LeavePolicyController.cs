// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/leave-policy")]
public sealed class LeavePolicyController(global::HRM.Services.Interfaces.LeaveType.ILeavePolicyService service) : HrmApiController
{
    [HttpGet("get-organisation-policies")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.LeaveTypes.LeavePolicyDto>>> GetOrganisationPolicies()
    {
        return Ok(await service.GetOrganisationPoliciesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-by-id")]
    public async Task<ActionResult<global::HRM.DTOs.LeaveTypes.LeavePolicyDto?>> GetById([FromQuery] global::System.Int32 policyId)
    {
        return Ok(await service.GetByIdAsync(policyId, HttpContext.RequestAborted));
    }

    [HttpPost("create")]
    public async Task<ActionResult<global::HRM.DTOs.LeaveTypes.ServiceResult<global::System.Int32>>> Create([FromBody] LeavePolicyCreateRequest request)
    {
        return Ok(await service.CreateAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class LeavePolicyCreateRequest
    {
        public global::HRM.DTOs.LeaveTypes.LeavePolicySaveRequest request { get; set; } = default!;
    }

    [HttpPost("update")]
    public async Task<ActionResult<global::HRM.DTOs.LeaveTypes.ServiceResult<global::System.Int32>>> Update([FromBody] LeavePolicyUpdateRequest request)
    {
        return Ok(await service.UpdateAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class LeavePolicyUpdateRequest
    {
        public global::HRM.DTOs.LeaveTypes.LeavePolicySaveRequest request { get; set; } = default!;
    }

    [HttpPost("deactivate")]
    public async Task<ActionResult<global::HRM.DTOs.LeaveTypes.ServiceResult<global::System.Int32>>> Deactivate([FromBody] LeavePolicyDeactivateRequest request)
    {
        return Ok(await service.DeactivateAsync(request.policyId, HttpContext.RequestAborted));
    }

    public sealed class LeavePolicyDeactivateRequest
    {
        public global::System.Int32 policyId { get; set; }
    }

}
