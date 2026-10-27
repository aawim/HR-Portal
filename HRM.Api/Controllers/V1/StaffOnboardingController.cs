// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-onboarding")]
public sealed class StaffOnboardingController(global::HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Employee.View", "Employee.Create")]
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.StaffOnboardingData>> Load([FromQuery] global::System.Int32? individualId = null)
    {
        return Ok(await service.LoadAsync(individualId));
    }

    [ApiPermission("Employee.Create")]
    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] StaffOnboardingSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.model));
    }

    public sealed class StaffOnboardingSubmitRequest
    {
        public global::HRM.DTOs.StaffOnboardingForm model { get; set; } = default!;
    }

    [HttpGet("get-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.StaffOnboardingRow>>> GetRequests([FromQuery] global::System.Int32? requestId = null, [FromQuery] global::System.Boolean pendingOnly = false)
    {
        return Ok(await service.GetRequestsAsync(requestId, pendingOnly));
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] StaffOnboardingActRequest request)
    {
        return Ok(await service.ActAsync(request.requestId, request.expectedState, request.action, request.comment));
    }

    public sealed class StaffOnboardingActRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

}
