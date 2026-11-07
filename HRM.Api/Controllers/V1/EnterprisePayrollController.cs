// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/enterprise-payroll")]
public sealed class EnterprisePayrollController(global::HRM.Application.Contracts.Pages.IEnterprisePayroll service) : HrmApiController
{
    [HttpGet("get-periods")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.PayrollPeriodRow>>> GetPeriods([FromQuery] global::System.Int32? year = null)
    {
        return Ok(await service.GetPeriodsAsync(year));
    }

    [HttpPost("create-period")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreatePeriod([FromBody] EnterprisePayrollCreatePeriodRequest request)
    {
        return Ok(await service.CreatePeriodAsync(request.input));
    }

    public sealed class EnterprisePayrollCreatePeriodRequest
    {
        public global::HRM.Application.Contracts.Pages.PayrollPeriodInput input { get; set; } = default!;
    }

    [HttpPost("update-period-status")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UpdatePeriodStatus([FromBody] EnterprisePayrollUpdatePeriodStatusRequest request)
    {
        return Ok(await service.UpdatePeriodStatusAsync(request.periodId, request.newStatus));
    }

    public sealed class EnterprisePayrollUpdatePeriodStatusRequest
    {
        public global::System.Int32 periodId { get; set; }
        public global::System.Int32 newStatus { get; set; }
    }

    [HttpGet("get-runs")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.PayrollRunSummaryRow>>> GetRuns([FromQuery] global::System.Int32? periodId = null)
    {
        return Ok(await service.GetRunsAsync(periodId));
    }

    [HttpGet("get-run-detail")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.PayrollRunSummaryRow>> GetRunDetail([FromQuery] global::System.Int32 runId)
    {
        return Ok(await service.GetRunDetailAsync(runId));
    }

    [HttpGet("get-progress")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.PayrollProgressRow>> GetProgress([FromQuery] global::System.Int32 runId)
    {
        return Ok(await service.GetProgressAsync(runId));
    }

    [HttpPost("create-run")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreateRun([FromBody] EnterprisePayrollCreateRunRequest request)
    {
        return Ok(await service.CreateRunAsync(request.input));
    }

    public sealed class EnterprisePayrollCreateRunRequest
    {
        public global::HRM.Application.Contracts.Pages.CreatePayrollRunInput input { get; set; } = default!;
    }

    [HttpPost("queue-calculation")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> QueueCalculation([FromBody] EnterprisePayrollQueueCalculationRequest request)
    {
        return Ok(await service.QueueCalculationAsync(request.runId));
    }

    public sealed class EnterprisePayrollQueueCalculationRequest
    {
        public global::System.Int32 runId { get; set; }
    }

    [HttpPost("submit-review")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitReview([FromBody] EnterprisePayrollSubmitReviewRequest request)
    {
        return Ok(await service.SubmitReviewAsync(request.runId, request.comment));
    }

    public sealed class EnterprisePayrollSubmitReviewRequest
    {
        public global::System.Int32 runId { get; set; }
        public global::System.String? comment { get; set; } = null;
    }

    [HttpPost("approve-payroll")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ApprovePayroll([FromBody] EnterprisePayrollApprovePayrollRequest request)
    {
        return Ok(await service.ApprovePayrollAsync(request.runId, request.comment));
    }

    public sealed class EnterprisePayrollApprovePayrollRequest
    {
        public global::System.Int32 runId { get; set; }
        public global::System.String? comment { get; set; } = null;
    }

    [HttpPost("finalise-payroll")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> FinalisePayroll([FromBody] EnterprisePayrollFinalisePayrollRequest request)
    {
        return Ok(await service.FinalisePayrollAsync(request.runId, request.comment));
    }

    public sealed class EnterprisePayrollFinalisePayrollRequest
    {
        public global::System.Int32 runId { get; set; }
        public global::System.String? comment { get; set; } = null;
    }

    [HttpPost("reject-payroll")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RejectPayroll([FromBody] EnterprisePayrollRejectPayrollRequest request)
    {
        return Ok(await service.RejectPayrollAsync(request.runId, request.reason));
    }

    public sealed class EnterprisePayrollRejectPayrollRequest
    {
        public global::System.Int32 runId { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpGet("get-employees")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.PayrollEmployeeRow>>> GetEmployees([FromQuery] global::System.Int32 runId, [FromQuery] global::System.Int32 page = 1, [FromQuery] global::System.Int32 pageSize = 50, [FromQuery] global::System.String? search = null)
    {
        return Ok(await service.GetEmployeesAsync(runId, page, pageSize, search));
    }

    [HttpGet("get-exceptions")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.PayrollExceptionRow>>> GetExceptions([FromQuery] global::System.Int32 runId)
    {
        return Ok(await service.GetExceptionsAsync(runId));
    }

    [HttpGet("get-payment-batches")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.PaymentBatchRow>>> GetPaymentBatches([FromQuery] global::System.Int32? runId = null)
    {
        return Ok(await service.GetPaymentBatchesAsync(runId));
    }

    [HttpPost("prepare-payment-batch")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.PaymentBatchRow>> PreparePaymentBatch([FromBody] EnterprisePayrollPreparePaymentBatchRequest request)
    {
        return Ok(await service.PreparePaymentBatchAsync(request.runId, request.bankCode, request.formatCode));
    }

    public sealed class EnterprisePayrollPreparePaymentBatchRequest
    {
        public global::System.Int32 runId { get; set; }
        public global::System.String bankCode { get; set; } = default!;
        public global::System.String formatCode { get; set; } = default!;
    }

    [HttpPost("export-payment-file")]
    public async Task<ActionResult<global::System.String>> ExportPaymentFile([FromBody] EnterprisePayrollExportPaymentFileRequest request)
    {
        return Ok(await service.ExportPaymentFileAsync(request.paymentBatchId));
    }

    public sealed class EnterprisePayrollExportPaymentFileRequest
    {
        public global::System.Int32 paymentBatchId { get; set; }
    }

    [HttpPost("reconcile-payment-batch")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ReconcilePaymentBatch([FromBody] EnterprisePayrollReconcilePaymentBatchRequest request)
    {
        return Ok(await service.ReconcilePaymentBatchAsync(request.paymentBatchId, request.reference));
    }

    public sealed class EnterprisePayrollReconcilePaymentBatchRequest
    {
        public global::System.Int32 paymentBatchId { get; set; }
        public global::System.String reference { get; set; } = default!;
    }

    [HttpGet("get-my-payslips")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.PayslipRow>>> GetMyPayslips()
    {
        return Ok(await service.GetMyPayslipsAsync());
    }

    [HttpGet("get-payslip-detail")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.PayslipRow>> GetPayslipDetail([FromQuery] global::System.Int32 payrollEmployeeId)
    {
        return Ok(await service.GetPayslipDetailAsync(payrollEmployeeId));
    }

}
