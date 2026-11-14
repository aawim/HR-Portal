using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.GoodsLoans;

namespace HRM.Application.Contracts.Pages;

public interface IGoodsLoanService
{
    // Stores
    Task<List<GoodsLoanStoreDto>> GetStoresAsync(int? organisationId = null, CancellationToken ct = default);
    Task<GoodsLoanStoreDto?> GetStoreByIdAsync(int storeId, CancellationToken ct = default);
    Task<ServiceResult> SaveStoreAsync(SaveGoodsLoanStoreRequest request, CancellationToken ct = default);
    Task<ServiceResult> ToggleStoreActiveAsync(int storeId, bool isActive, CancellationToken ct = default);

    // Store Agreements
    Task<List<GoodsLoanStoreAgreementDto>> GetAgreementsAsync(int? organisationId = null, int? storeId = null, CancellationToken ct = default);
    Task<GoodsLoanStoreAgreementDto?> GetAgreementByIdAsync(int agreementId, CancellationToken ct = default);
    Task<ServiceResult> SaveAgreementAsync(SaveGoodsLoanStoreAgreementRequest request, CancellationToken ct = default);
    Task<ServiceResult> ToggleAgreementActiveAsync(int agreementId, bool isActive, CancellationToken ct = default);

    // Deduction Policies
    Task<List<GoodsLoanDeductionPolicyDto>> GetPoliciesAsync(int? organisationId = null, CancellationToken ct = default);
    Task<GoodsLoanDeductionPolicyDto?> GetActivePolicyAsync(int organisationId, CancellationToken ct = default);
    Task<ServiceResult> SavePolicyAsync(SaveGoodsLoanDeductionPolicyRequest request, CancellationToken ct = default);

    // Eligibility & Calculations
    Task<GoodsLoanEligibilityResult> CheckEligibilityAsync(GoodsLoanEligibilityCheckRequest request, CancellationToken ct = default);
    Task<List<RepaymentScheduleInstalmentPlan>> CalculateSchedulePreviewAsync(RepaymentScheduleCalculationRequest request, CancellationToken ct = default);

    // Loans & Staff Portal
    Task<GoodsLoanStaffWorkspaceSummary> GetStaffWorkspaceSummaryAsync(int individualId, int? organisationId = null, CancellationToken ct = default);
    Task<List<GoodsLoanDto>> GetLoansAsync(GoodsLoanFilterRequest filter, CancellationToken ct = default);
    Task<GoodsLoanDto?> GetLoanByIdAsync(int loanId, CancellationToken ct = default);
    Task<ServiceResult> ApplyForLoanAsync(CreateGoodsLoanApplicationRequest request, CancellationToken ct = default);
    Task<ServiceResult> ApproveLoanAsync(int loanId, int approverUserId, string? remarks = null, CancellationToken ct = default);
    Task<ServiceResult> RejectLoanAsync(int loanId, int rejectorUserId, string reason, CancellationToken ct = default);
    Task<ServiceResult> CancelLoanAsync(int loanId, int cancellerUserId, string reason, CancellationToken ct = default);
    Task<ServiceResult> ProcessEarlySettlementAsync(EarlyLoanSettlementRequest request, int userId, CancellationToken ct = default);

    // Store Settlements (Finance)
    Task<List<GoodsLoanStoreSettlementDto>> GetStoreSettlementsAsync(int organisationId, int? storeId = null, CancellationToken ct = default);
    Task<ServiceResult> CreateStoreSettlementBatchAsync(CreateStoreSettlementBatchRequest request, int userId, CancellationToken ct = default);
    Task<ServiceResult> ApproveStoreSettlementAsync(int settlementID, int userId, CancellationToken ct = default);
    Task<ServiceResult> ConfirmStoreSettlementPaidAsync(int settlementID, int userId, string paymentReference, CancellationToken ct = default);

    // Reports
    Task<GoodsLoanDto?> GetEmployeeLoanStatementAsync(int loanId, CancellationToken ct = default);
    Task<List<GoodsLoanDto>> GetStoreMonthlyDeductionsReportAsync(int organisationId, DateTime month, CancellationToken ct = default);
    Task<List<GoodsLoanDto>> GetOutstandingLoansReportAsync(int organisationId, CancellationToken ct = default);
}
