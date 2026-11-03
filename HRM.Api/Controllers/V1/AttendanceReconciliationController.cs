// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-reconciliation")]
public sealed class AttendanceReconciliationController(global::HRM.Services.Attendance.Reconciliation.IAttendanceReconciliationService service) : HrmApiController
{
    [HttpPost("reconcile")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceReconciliationResult>> Reconcile([FromBody] AttendanceReconciliationReconcileRequest request)
    {
        return Ok(await service.ReconcileAsync(request.individualId, request.jobId, request.organisationId, request.workDate, HttpContext.RequestAborted));
    }

    [HttpPost("reconcile-range")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceBatchReconciliationResult>> ReconcileRange([FromBody] AttendanceReconciliationReconcileRangeRequest request)
    {
        return Ok(await service.ReconcileRangeAsync(request.request, HttpContext.RequestAborted));
    }

    [HttpPost("apply-manual-override")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceReconciliationResult>> ApplyManualOverride([FromBody] AttendanceReconciliationApplyManualOverrideRequest request)
    {
        return Ok(await service.ApplyManualOverrideAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class AttendanceReconciliationReconcileRequest
    {
        public global::System.Int32 individualId { get; set; }
        public global::System.Int32 jobId { get; set; }
        public global::System.Int32 organisationId { get; set; }
        public global::System.DateOnly workDate { get; set; }
    }

    public sealed class AttendanceReconciliationReconcileRangeRequest
    {
        public global::HRM.DTOs.Attendance.AttendanceBatchReconciliationRequest request { get; set; } = default!;
    }

    public sealed class AttendanceReconciliationApplyManualOverrideRequest
    {
        public global::HRM.DTOs.Attendance.AttendanceManualOverrideRequest request { get; set; } = default!;
    }
}
