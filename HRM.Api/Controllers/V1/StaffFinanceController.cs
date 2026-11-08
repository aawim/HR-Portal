// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-finance")]
public sealed class StaffFinanceController(global::HRM.Application.Contracts.Pages.IStaffFinance service) : HrmApiController
{
    [HttpGet("get-configuration")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.FinanceConfiguration>> GetConfiguration()
    {
        return Ok(await service.GetConfigurationAsync());
    }

    [HttpPost("save-allowance-type")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveAllowanceType([FromBody] StaffFinanceSaveAllowanceTypeRequest request)
    {
        return Ok(await service.SaveAllowanceTypeAsync(request.input));
    }

    public sealed class StaffFinanceSaveAllowanceTypeRequest
    {
        public global::HRM.Application.Contracts.Pages.AllowanceTypeInput input { get; set; } = default!;
    }

    [HttpPost("add-position-rule")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AddPositionRule([FromBody] StaffFinanceAddPositionRuleRequest request)
    {
        return Ok(await service.AddPositionRuleAsync(request.input));
    }

    public sealed class StaffFinanceAddPositionRuleRequest
    {
        public global::HRM.Application.Contracts.Pages.PositionAllowanceInput input { get; set; } = default!;
    }

    [HttpPost("end-position-rule")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> EndPositionRule([FromBody] StaffFinanceEndPositionRuleRequest request)
    {
        return Ok(await service.EndPositionRuleAsync(request.id, request.lastEntitlementDate));
    }

    public sealed class StaffFinanceEndPositionRuleRequest
    {
        public global::System.Int64 id { get; set; }
        public global::System.DateTime lastEntitlementDate { get; set; }
    }

    [HttpPost("add-deduction-policy")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AddDeductionPolicy([FromBody] StaffFinanceAddDeductionPolicyRequest request)
    {
        return Ok(await service.AddDeductionPolicyAsync(request.input));
    }

    public sealed class StaffFinanceAddDeductionPolicyRequest
    {
        public global::HRM.Application.Contracts.Pages.DeductionPolicyInput input { get; set; } = default!;
    }

    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.FinanceWorkspace>> Load([FromQuery] global::System.Int32? staffId = null)
    {
        return Ok(await service.LoadAsync(staffId));
    }

    [HttpPost("query/preview-agreement")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.FinanceInstalmentRow>>> PreviewAgreement([FromBody] StaffFinancePreviewAgreementRequest request)
    {
        return Ok(await service.PreviewAgreementAsync(request.input));
    }

    public sealed class StaffFinancePreviewAgreementRequest
    {
        public global::HRM.Application.Contracts.Pages.FinanceApplication input { get; set; } = default!;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] StaffFinanceSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.input));
    }

    public sealed class StaffFinanceSubmitRequest
    {
        public global::HRM.Application.Contracts.Pages.FinanceApplication input { get; set; } = default!;
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] StaffFinanceActRequest request)
    {
        return Ok(await service.ActAsync(request.requestId, request.expectedState, request.action, request.comment));
    }

    public sealed class StaffFinanceActRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

    [HttpPost("assess-allowance")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AssessAllowance([FromBody] StaffFinanceAssessAllowanceRequest request)
    {
        return Ok(await service.AssessAllowanceAsync(request.requestId, request.expectedState, request.amount, request.currencyTypeId));
    }

    public sealed class StaffFinanceAssessAllowanceRequest
    {
        public global::System.Int32 requestId { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.Decimal amount { get; set; }
        public global::System.Int32 currencyTypeId { get; set; }
    }

    [HttpPost("upload-document")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UploadDocument([FromBody] StaffFinanceUploadDocumentRequest request)
    {
        return Ok(await service.UploadDocumentAsync(request.input));
    }

    public sealed class StaffFinanceUploadDocumentRequest
    {
        public global::HRM.Application.Contracts.Pages.FinanceDocumentUpload input { get; set; } = default!;
    }

    [HttpPost("download-document")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.FinanceDocumentDownload>> DownloadDocument([FromBody] StaffFinanceDownloadDocumentRequest request)
    {
        return Ok(await service.DownloadDocumentAsync(request.id));
    }

    public sealed class StaffFinanceDownloadDocumentRequest
    {
        public global::System.Int64 id { get; set; }
    }

    [HttpPost("query/preview-payroll")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.FinancePayrollPreview>> PreviewPayroll([FromBody] StaffFinancePreviewPayrollRequest request)
    {
        return Ok(await service.PreviewPayrollAsync(request.month, request.jobIds));
    }

    public sealed class StaffFinancePreviewPayrollRequest
    {
        public global::System.DateTime month { get; set; }
        public global::System.Collections.Generic.List<global::System.Int32> jobIds { get; set; } = default!;
    }

    [HttpGet("get-payroll-cycles")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.FinanceOption>>> GetPayrollCycles([FromQuery] global::System.DateTime month)
    {
        return Ok(await service.GetPayrollCyclesAsync(month));
    }

    [HttpPost("post-payroll")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> PostPayroll([FromBody] StaffFinancePostPayrollRequest request)
    {
        return Ok(await service.PostPayrollAsync(request.input));
    }

    public sealed class StaffFinancePostPayrollRequest
    {
        public global::HRM.Application.Contracts.Pages.FinancePayrollInput input { get; set; } = default!;
    }

    [HttpPost("payroll-history")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.FinancePayrollHistory>>> PayrollHistory([FromBody] StaffFinancePayrollHistoryRequest request)
    {
        return Ok(await service.PayrollHistoryAsync(request.staffId));
    }

    public sealed class StaffFinancePayrollHistoryRequest
    {
        public global::System.Int32? staffId { get; set; } = null;
    }

}
