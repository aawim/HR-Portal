using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.Contracts.Pages;
using HRM.DTOs;
using HRM.DTOs.GoodsLoans;

namespace HRM.UI.Services.GoodsLoans;

public sealed class GoodsLoanApiClient(HttpClient client) : IGoodsLoanService
{
    #region Stores

    public async Task<List<GoodsLoanStoreDto>> GetStoresAsync(int? organisationId = null, CancellationToken ct = default)
    {
        try
        {
            var url = "/api/goods-loans/stores";
            if (organisationId.HasValue) url += $"?organisationId={organisationId.Value}";
            return await client.GetFromJsonAsync<List<GoodsLoanStoreDto>>(url, ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetStoresAsync error: {ex.Message}");
            return new();
        }
    }

    public async Task<GoodsLoanStoreDto?> GetStoreByIdAsync(int storeId, CancellationToken ct = default)
    {
        try
        {
            return await client.GetFromJsonAsync<GoodsLoanStoreDto>($"/api/goods-loans/stores/{storeId}", ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetStoreByIdAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<ServiceResult> SaveStoreAsync(SaveGoodsLoanStoreRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/stores", request, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> ToggleStoreActiveAsync(int storeId, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync($"/api/goods-loans/stores/{storeId}/toggle-active", new { IsActive = isActive }, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion

    #region Store Agreements

    public async Task<List<GoodsLoanStoreAgreementDto>> GetAgreementsAsync(int? organisationId = null, int? storeId = null, CancellationToken ct = default)
    {
        try
        {
            var url = "/api/goods-loans/agreements";
            var query = new List<string>();
            if (organisationId.HasValue) query.Add($"organisationId={organisationId.Value}");
            if (storeId.HasValue) query.Add($"storeId={storeId.Value}");
            if (query.Count > 0) url += "?" + string.Join("&", query);
            return await client.GetFromJsonAsync<List<GoodsLoanStoreAgreementDto>>(url, ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetAgreementsAsync error: {ex.Message}");
            return new();
        }
    }

    public async Task<GoodsLoanStoreAgreementDto?> GetAgreementByIdAsync(int agreementId, CancellationToken ct = default)
    {
        try
        {
            return await client.GetFromJsonAsync<GoodsLoanStoreAgreementDto>($"/api/goods-loans/agreements/{agreementId}", ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetAgreementByIdAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<ServiceResult> SaveAgreementAsync(SaveGoodsLoanStoreAgreementRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/agreements", request, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> ToggleAgreementActiveAsync(int agreementId, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync($"/api/goods-loans/agreements/{agreementId}/toggle-active", new { IsActive = isActive }, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion

    #region Deduction Policies

    public async Task<List<GoodsLoanDeductionPolicyDto>> GetPoliciesAsync(int? organisationId = null, CancellationToken ct = default)
    {
        try
        {
            var url = "/api/goods-loans/policies";
            if (organisationId.HasValue) url += $"?organisationId={organisationId.Value}";
            return await client.GetFromJsonAsync<List<GoodsLoanDeductionPolicyDto>>(url, ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetPoliciesAsync error: {ex.Message}");
            return new();
        }
    }

    public async Task<GoodsLoanDeductionPolicyDto?> GetActivePolicyAsync(int organisationId, CancellationToken ct = default)
    {
        try
        {
            return await client.GetFromJsonAsync<GoodsLoanDeductionPolicyDto>($"/api/goods-loans/policies/active/{organisationId}", ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetActivePolicyAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<ServiceResult> SavePolicyAsync(SaveGoodsLoanDeductionPolicyRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/policies", request, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion

    #region Eligibility & Calculations

    public async Task<GoodsLoanEligibilityResult> CheckEligibilityAsync(GoodsLoanEligibilityCheckRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/check-eligibility", request, ct);
            return await resp.Content.ReadFromJsonAsync<GoodsLoanEligibilityResult>(cancellationToken: ct)
                   ?? new GoodsLoanEligibilityResult { IsEligible = false, ValidationErrors = new() { "Empty response." } };
        }
        catch (Exception ex)
        {
            return new GoodsLoanEligibilityResult { IsEligible = false, ValidationErrors = new() { ex.Message } };
        }
    }

    public async Task<List<RepaymentScheduleInstalmentPlan>> CalculateSchedulePreviewAsync(RepaymentScheduleCalculationRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/calculate-schedule", request, ct);
            return await resp.Content.ReadFromJsonAsync<List<RepaymentScheduleInstalmentPlan>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] CalculateSchedulePreviewAsync error: {ex.Message}");
            return new();
        }
    }

    #endregion

    #region Staff Portal & Loans

    public async Task<GoodsLoanStaffWorkspaceSummary> GetStaffWorkspaceSummaryAsync(int individualId, int? organisationId = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/goods-loans/staff-summary/{individualId}";
            if (organisationId.HasValue) url += $"?organisationId={organisationId.Value}";
            return await client.GetFromJsonAsync<GoodsLoanStaffWorkspaceSummary>(url, ct)
                   ?? new GoodsLoanStaffWorkspaceSummary();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetStaffWorkspaceSummaryAsync error: {ex.Message}");
            return new GoodsLoanStaffWorkspaceSummary();
        }
    }

    public async Task<List<GoodsLoanDto>> GetLoansAsync(GoodsLoanFilterRequest filter, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/loans/search", filter, ct);
            return await resp.Content.ReadFromJsonAsync<List<GoodsLoanDto>>(cancellationToken: ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetLoansAsync error: {ex.Message}");
            return new();
        }
    }

    public async Task<GoodsLoanDto?> GetLoanByIdAsync(int loanId, CancellationToken ct = default)
    {
        try
        {
            return await client.GetFromJsonAsync<GoodsLoanDto>($"/api/goods-loans/loans/{loanId}", ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetLoanByIdAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<ServiceResult> ApplyForLoanAsync(CreateGoodsLoanApplicationRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/applications", request, ct);
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    return ServiceResult.Fail("Goods loans can only be entered by HR or administrative staff through the Admin Portal.");
                var text = await resp.Content.ReadAsStringAsync(ct);
                return ServiceResult.Fail(string.IsNullOrWhiteSpace(text) ? $"Request failed with status {resp.StatusCode}" : text);
            }
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> ApproveLoanAsync(int loanId, int approverUserId, string? remarks = null, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync($"/api/goods-loans/loans/{loanId}/approve", new { Remarks = remarks }, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> RejectLoanAsync(int loanId, int rejectorUserId, string reason, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync($"/api/goods-loans/loans/{loanId}/reject", new { Remarks = reason }, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> CancelLoanAsync(int loanId, int cancellerUserId, string reason, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync($"/api/goods-loans/loans/{loanId}/cancel", new { Remarks = reason }, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> ProcessEarlySettlementAsync(EarlyLoanSettlementRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/loans/early-settlement", request, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion

    #region Store Settlements

    public async Task<List<GoodsLoanStoreSettlementDto>> GetStoreSettlementsAsync(int organisationId, int? storeId = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/goods-loans/settlements?organisationId={organisationId}";
            if (storeId.HasValue) url += $"&storeId={storeId.Value}";
            return await client.GetFromJsonAsync<List<GoodsLoanStoreSettlementDto>>(url, ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetStoreSettlementsAsync error: {ex.Message}");
            return new();
        }
    }

    public async Task<ServiceResult> CreateStoreSettlementBatchAsync(CreateStoreSettlementBatchRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/goods-loans/settlements/batches", request, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> ApproveStoreSettlementAsync(int settlementID, int userId, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync($"/api/goods-loans/settlements/{settlementID}/approve", new { }, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> ConfirmStoreSettlementPaidAsync(int settlementID, int userId, string paymentReference, CancellationToken ct = default)
    {
        try
        {
            var resp = await client.PostAsJsonAsync($"/api/goods-loans/settlements/{settlementID}/confirm-paid", new { PaymentReference = paymentReference }, ct);
            return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
                   ?? ServiceResult.Fail("No response from server.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion

    #region Reports

    public async Task<GoodsLoanDto?> GetEmployeeLoanStatementAsync(int loanId, CancellationToken ct = default)
    {
        try
        {
            return await client.GetFromJsonAsync<GoodsLoanDto>($"/api/goods-loans/reports/loan-statement/{loanId}", ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetEmployeeLoanStatementAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<GoodsLoanDto>> GetStoreMonthlyDeductionsReportAsync(int organisationId, DateTime month, CancellationToken ct = default)
    {
        try
        {
            return await client.GetFromJsonAsync<List<GoodsLoanDto>>($"/api/goods-loans/reports/monthly-deductions?organisationId={organisationId}&month={month:yyyy-MM-dd}", ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetStoreMonthlyDeductionsReportAsync error: {ex.Message}");
            return new();
        }
    }

    public async Task<List<GoodsLoanDto>> GetOutstandingLoansReportAsync(int organisationId, CancellationToken ct = default)
    {
        try
        {
            return await client.GetFromJsonAsync<List<GoodsLoanDto>>($"/api/goods-loans/reports/outstanding-loans?organisationId={organisationId}", ct) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoodsLoanApiClient] GetOutstandingLoansReportAsync error: {ex.Message}");
            return new();
        }
    }

    #endregion
}
