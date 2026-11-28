// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-deduction")]
public sealed class AttendanceDeductionController(global::HRM.Services.Interfaces.IAttendanceDeductionService service) : HrmApiController
{
    [HttpGet("get-effective-policy")]
    public async Task<ActionResult<global::HRM.Models.AttendanceDeductions.AttendanceDeductionPolicy>> GetEffectivePolicy([FromQuery] global::System.Int32 organisationId, [FromQuery] global::System.DateOnly workDate)
    {
        return Ok(await service.GetEffectivePolicyAsync(organisationId, workDate));
    }

    [HttpPost("evaluate-daily-deduction")]
    public async Task<ActionResult<global::HRM.Models.AttendanceDeductions.AttendanceAdjustment?>> EvaluateDailyDeduction([FromBody] AttendanceDeductionEvaluateDailyDeductionRequest request)
    {
        return Ok(await service.EvaluateDailyDeductionAsync(request.organisationId, request.individualId, request.jobId, request.workDate, HttpContext.RequestAborted));
    }

    public sealed class AttendanceDeductionEvaluateDailyDeductionRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Int32 individualId { get; set; }
        public global::System.Int32 jobId { get; set; }
        public global::System.DateOnly workDate { get; set; }
    }

    [HttpPost("evaluate-range")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Models.AttendanceDeductions.AttendanceAdjustment>>> EvaluateRange([FromBody] AttendanceDeductionEvaluateRangeRequest request)
    {
        return Ok(await service.EvaluateRangeAsync(request.organisationId, request.individualId, request.jobId, request.fromDate, request.toDate, HttpContext.RequestAborted));
    }

    public sealed class AttendanceDeductionEvaluateRangeRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Int32? individualId { get; set; }
        public global::System.Int32? jobId { get; set; }
        public global::System.DateOnly fromDate { get; set; }
        public global::System.DateOnly toDate { get; set; }
    }

    [HttpGet("get-adjustments")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.AttendanceDeductions.AttendanceAdjustmentDto>>> GetAdjustments([FromQuery] global::System.Int32 organisationId, [FromQuery] global::System.DateOnly? fromDate = null, [FromQuery] global::System.DateOnly? toDate = null, [FromQuery] global::System.String? verificationStatus = null, [FromQuery] global::System.String? payrollStatus = null)
    {
        return Ok(await service.GetAdjustmentsAsync(organisationId, fromDate, toDate, verificationStatus, payrollStatus));
    }

    [HttpPost("verify-adjustment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> VerifyAdjustment([FromBody] AttendanceDeductionVerifyAdjustmentRequest request)
    {
        return Ok(await service.VerifyAdjustmentAsync(request.adjustmentId, request.userId, request.comments));
    }

    public sealed class AttendanceDeductionVerifyAdjustmentRequest
    {
        public global::System.Int64 adjustmentId { get; set; }
        public global::System.Int32 userId { get; set; }
        public global::System.String comments { get; set; } = default!;
    }

    [HttpPost("excuse-adjustment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ExcuseAdjustment([FromBody] AttendanceDeductionExcuseAdjustmentRequest request)
    {
        return Ok(await service.ExcuseAdjustmentAsync(request.adjustmentId, request.userId, request.reason));
    }

    public sealed class AttendanceDeductionExcuseAdjustmentRequest
    {
        public global::System.Int64 adjustmentId { get; set; }
        public global::System.Int32 userId { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpPost("reject-adjustment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RejectAdjustment([FromBody] AttendanceDeductionRejectAdjustmentRequest request)
    {
        return Ok(await service.RejectAdjustmentAsync(request.adjustmentId, request.userId, request.reason));
    }

    public sealed class AttendanceDeductionRejectAdjustmentRequest
    {
        public global::System.Int64 adjustmentId { get; set; }
        public global::System.Int32 userId { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpGet("get-policies")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.AttendanceDeductions.AttendanceDeductionPolicyDto>>> GetPolicies([FromQuery] global::System.Int32 organisationId)
    {
        return Ok(await service.GetPoliciesAsync(organisationId));
    }

    [HttpPost("save-policy")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SavePolicy([FromBody] AttendanceDeductionSavePolicyRequest request)
    {
        return Ok(await service.SavePolicyAsync(request.policy, request.userId));
    }

    public sealed class AttendanceDeductionSavePolicyRequest
    {
        public global::HRM.Models.AttendanceDeductions.AttendanceDeductionPolicy policy { get; set; } = default!;
        public global::System.Int32 userId { get; set; }
    }

    [HttpGet("preview-calculation")]
    public async Task<ActionResult<global::HRM.DTOs.AttendanceDeductions.AttendanceDeductionCalculationPreviewDto>> PreviewCalculation([FromQuery] global::System.Int32 organisationId, [FromQuery] global::System.Int32 jobId, [FromQuery] global::System.DateOnly workDate, [FromQuery] global::System.Int32 lateMinutes, [FromQuery] global::System.String kind)
    {
        return Ok(await service.PreviewCalculationAsync(organisationId, jobId, workDate, lateMinutes, kind));
    }

}
