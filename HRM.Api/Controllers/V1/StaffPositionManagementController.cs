// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-position-management")]
public sealed class StaffPositionManagementController(global::HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Services.Contracts.StaffPositionManagementServiceContracts.Data>> Load([FromQuery] global::System.Int32 staffId)
    {
        return Ok(await service.LoadAsync(staffId));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] StaffPositionManagementSaveRequest request)
    {
        await service.SaveAsync(request.staffId, request.jobId, request.positionId, request.from, request.to, request.reason, request.original, request.change, request.remove, request.jobMatrixStepId);
        return NoContent();
    }

    public sealed class StaffPositionManagementSaveRequest
    {
        public global::System.Int32 staffId { get; set; }
        public global::System.Int32 jobId { get; set; }
        public global::System.Int32 positionId { get; set; }
        public global::System.DateTime from { get; set; }
        public global::System.DateTime? to { get; set; }
        public global::System.String reason { get; set; } = default!;
        public global::HRM.Services.Contracts.StaffPositionManagementServiceContracts.Assignment? original { get; set; } = null;
        public global::System.Boolean change { get; set; } = false;
        public global::System.Boolean remove { get; set; } = false;
        public global::System.Int32? jobMatrixStepId { get; set; } = null;
    }

    [HttpGet("load-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.StaffPositionRequestRow>>> LoadRequests([FromQuery] global::System.Int32? staffId = null, [FromQuery] global::System.Boolean pendingOnly = false)
    {
        return Ok(await service.LoadRequestsAsync(staffId, pendingOnly));
    }

    [HttpPost("act-on-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ActOnRequest([FromBody] StaffPositionManagementActOnRequestRequest request)
    {
        return Ok(await service.ActOnRequestAsync(request.id, request.expectedState, request.action, request.comment));
    }

    public sealed class StaffPositionManagementActOnRequestRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

}
