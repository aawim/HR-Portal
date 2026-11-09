using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Pages;
using HRM.DTOs;
using HRM.DTOs.GoodsLoans;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/goods-loans")]
public sealed class GoodsLoansController(IGoodsLoanService service, IUserAccessService access) : HrmApiController
{
    private async Task<int> GetCurrentUserIdAsync()
    {
        var context = await access.RequireContextAsync();
        return context.UserId > 0 ? context.UserId : context.IndividualId;
    }

    #region Stores

    [HttpGet("stores")]
    public async Task<ActionResult<List<GoodsLoanStoreDto>>> GetStores([FromQuery] int? organisationId = null, CancellationToken ct = default)
    {
        var result = await service.GetStoresAsync(organisationId, ct);
        return Ok(result);
    }

    [HttpGet("stores/{id:int}")]
    public async Task<ActionResult<GoodsLoanStoreDto>> GetStoreById(int id, CancellationToken ct = default)
    {
        var result = await service.GetStoreByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("stores")]
    public async Task<ActionResult<ServiceResult>> SaveStore([FromBody] SaveGoodsLoanStoreRequest request, CancellationToken ct = default)
    {
        var result = await service.SaveStoreAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("stores/{id:int}/toggle-active")]
    public async Task<ActionResult<ServiceResult>> ToggleStoreActive(int id, [FromBody] ToggleActiveRequest req, CancellationToken ct = default)
    {
        var result = await service.ToggleStoreActiveAsync(id, req.IsActive, ct);
        return Ok(result);
    }

    #endregion

    #region Store Agreements

    [HttpGet("agreements")]
    public async Task<ActionResult<List<GoodsLoanStoreAgreementDto>>> GetAgreements([FromQuery] int? organisationId = null, [FromQuery] int? storeId = null, CancellationToken ct = default)
    {
        var result = await service.GetAgreementsAsync(organisationId, storeId, ct);
        return Ok(result);
    }

    [HttpGet("agreements/{id:int}")]
    public async Task<ActionResult<GoodsLoanStoreAgreementDto>> GetAgreementById(int id, CancellationToken ct = default)
    {
        var result = await service.GetAgreementByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("agreements")]
    public async Task<ActionResult<ServiceResult>> SaveAgreement([FromBody] SaveGoodsLoanStoreAgreementRequest request, CancellationToken ct = default)
    {
        var result = await service.SaveAgreementAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("agreements/{id:int}/toggle-active")]
    public async Task<ActionResult<ServiceResult>> ToggleAgreementActive(int id, [FromBody] ToggleActiveRequest req, CancellationToken ct = default)
    {
        var result = await service.ToggleAgreementActiveAsync(id, req.IsActive, ct);
        return Ok(result);
    }

    #endregion

    #region Deduction Policies

    [HttpGet("policies")]
    public async Task<ActionResult<List<GoodsLoanDeductionPolicyDto>>> GetPolicies([FromQuery] int? organisationId = null, CancellationToken ct = default)
    {
        var result = await service.GetPoliciesAsync(organisationId, ct);
        return Ok(result);
    }

    [HttpGet("policies/active/{organisationId:int}")]
    public async Task<ActionResult<GoodsLoanDeductionPolicyDto>> GetActivePolicy(int organisationId, CancellationToken ct = default)
    {
        var result = await service.GetActivePolicyAsync(organisationId, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("policies")]
    public async Task<ActionResult<ServiceResult>> SavePolicy([FromBody] SaveGoodsLoanDeductionPolicyRequest request, CancellationToken ct = default)
    {
        var result = await service.SavePolicyAsync(request, ct);
        return Ok(result);
    }

    #endregion

    #region Eligibility & Calculations

    [HttpPost("check-eligibility")]
    public async Task<ActionResult<GoodsLoanEligibilityResult>> CheckEligibility([FromBody] GoodsLoanEligibilityCheckRequest request, CancellationToken ct = default)
    {
        var result = await service.CheckEligibilityAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("calculate-schedule")]
    public async Task<ActionResult<List<RepaymentScheduleInstalmentPlan>>> CalculateSchedulePreview([FromBody] RepaymentScheduleCalculationRequest request, CancellationToken ct = default)
    {
        var result = await service.CalculateSchedulePreviewAsync(request, ct);
        return Ok(result);
    }

    #endregion

    #region Staff Portal & Loans

    [HttpGet("staff-summary/{individualId:int}")]
    public async Task<ActionResult<GoodsLoanStaffWorkspaceSummary>> GetStaffWorkspaceSummary(int individualId, [FromQuery] int? organisationId = null, CancellationToken ct = default)
    {
        var result = await service.GetStaffWorkspaceSummaryAsync(individualId, organisationId, ct);
        return Ok(result);
    }

    [HttpPost("loans/search")]
    public async Task<ActionResult<List<GoodsLoanDto>>> GetLoans([FromBody] GoodsLoanFilterRequest filter, CancellationToken ct = default)
    {
        var result = await service.GetLoansAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("loans/{id:int}")]
    public async Task<ActionResult<GoodsLoanDto>> GetLoanById(int id, CancellationToken ct = default)
    {
        var result = await service.GetLoanByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [ApiPermission("GoodsLoan.Manage", "GoodsLoan.Create", "Admin")]
    [HttpPost("applications")]
    public async Task<ActionResult<ServiceResult>> ApplyForLoan([FromBody] CreateGoodsLoanApplicationRequest request, CancellationToken ct = default)
    {
        var result = await service.ApplyForLoanAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("loans/{id:int}/approve")]
    public async Task<ActionResult<ServiceResult>> ApproveLoan(int id, [FromBody] LoanActionRequest? req, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync();
        var result = await service.ApproveLoanAsync(id, userId, req?.Remarks, ct);
        return Ok(result);
    }

    [HttpPost("loans/{id:int}/reject")]
    public async Task<ActionResult<ServiceResult>> RejectLoan(int id, [FromBody] LoanActionRequest req, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync();
        var result = await service.RejectLoanAsync(id, userId, req.Remarks ?? "Rejected", ct);
        return Ok(result);
    }

    [HttpPost("loans/{id:int}/cancel")]
    public async Task<ActionResult<ServiceResult>> CancelLoan(int id, [FromBody] LoanActionRequest req, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync();
        var result = await service.CancelLoanAsync(id, userId, req.Remarks ?? "Cancelled", ct);
        return Ok(result);
    }

    [HttpPost("loans/early-settlement")]
    public async Task<ActionResult<ServiceResult>> ProcessEarlySettlement([FromBody] EarlyLoanSettlementRequest request, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync();
        var result = await service.ProcessEarlySettlementAsync(request, userId, ct);
        return Ok(result);
    }

    #endregion

    #region Store Settlements

    [HttpGet("settlements")]
    public async Task<ActionResult<List<GoodsLoanStoreSettlementDto>>> GetStoreSettlements([FromQuery] int organisationId, [FromQuery] int? storeId = null, CancellationToken ct = default)
    {
        var result = await service.GetStoreSettlementsAsync(organisationId, storeId, ct);
        return Ok(result);
    }

    [HttpPost("settlements/batches")]
    public async Task<ActionResult<ServiceResult>> CreateStoreSettlementBatch([FromBody] CreateStoreSettlementBatchRequest request, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync();
        var result = await service.CreateStoreSettlementBatchAsync(request, userId, ct);
        return Ok(result);
    }

    [HttpPost("settlements/{id:int}/approve")]
    public async Task<ActionResult<ServiceResult>> ApproveStoreSettlement(int id, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync();
        var result = await service.ApproveStoreSettlementAsync(id, userId, ct);
        return Ok(result);
    }

    [HttpPost("settlements/{id:int}/confirm-paid")]
    public async Task<ActionResult<ServiceResult>> ConfirmStoreSettlementPaid(int id, [FromBody] ConfirmPaidRequest req, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync();
        var result = await service.ConfirmStoreSettlementPaidAsync(id, userId, req.PaymentReference, ct);
        return Ok(result);
    }

    #endregion

    #region Reports

    [HttpGet("reports/loan-statement/{id:int}")]
    public async Task<ActionResult<GoodsLoanDto>> GetEmployeeLoanStatement(int id, CancellationToken ct = default)
    {
        var result = await service.GetEmployeeLoanStatementAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("reports/monthly-deductions")]
    public async Task<ActionResult<List<GoodsLoanDto>>> GetStoreMonthlyDeductionsReport([FromQuery] int organisationId, [FromQuery] DateTime month, CancellationToken ct = default)
    {
        var result = await service.GetStoreMonthlyDeductionsReportAsync(organisationId, month, ct);
        return Ok(result);
    }

    [HttpGet("reports/outstanding-loans")]
    public async Task<ActionResult<List<GoodsLoanDto>>> GetOutstandingLoansReport([FromQuery] int organisationId, CancellationToken ct = default)
    {
        var result = await service.GetOutstandingLoansReportAsync(organisationId, ct);
        return Ok(result);
    }

    #endregion
}

public sealed class ToggleActiveRequest
{
    public bool IsActive { get; set; }
}

public sealed class LoanActionRequest
{
    public string? Remarks { get; set; }
}

public sealed class ConfirmPaidRequest
{
    public string PaymentReference { get; set; } = string.Empty;
}
