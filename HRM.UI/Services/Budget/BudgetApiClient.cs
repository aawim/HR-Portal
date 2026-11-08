using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.DTOs.Budget;
using HRM.Application.Interfaces.Budget;
using HRM.Services.Exports;

namespace HRM.UI.Services.Budget;

public sealed class BudgetApiClient(HttpClient client) : IBudgetService
{
    public async Task<BudgetSummaryDto> GetBudgetSummaryAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<BudgetSummaryDto>($"/api/budget/summary?fiscalYear={fiscalYear}&organisationId={organisationId}", ct);
            return result ?? new BudgetSummaryDto();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching summary: {ex.Message}");
            return new BudgetSummaryDto { FiscalYear = fiscalYear };
        }
    }

    public async Task<List<BudgetAllocationDto>> GetAllocationsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<BudgetAllocationDto>>($"/api/budget/allocations?fiscalYear={fiscalYear}&organisationId={organisationId}", ct);
            return result ?? new List<BudgetAllocationDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching allocations: {ex.Message}");
            return new List<BudgetAllocationDto>();
        }
    }

    public async Task<List<BudgetItemDto>> GetBudgetItemsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<BudgetItemDto>>("/api/budget/items", ct);
            return result ?? new List<BudgetItemDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching budget items: {ex.Message}");
            return new List<BudgetItemDto>();
        }
    }

    public async Task<BudgetAllocationDto> SaveAllocationAsync(CreateBudgetAllocationRequest request, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/budget/allocations", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetAllocationDto>(cancellationToken: ct);
        return result!;
    }

    public async Task<List<BudgetVirementDto>> GetVirementsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<BudgetVirementDto>>($"/api/budget/virements?fiscalYear={fiscalYear}&organisationId={organisationId}", ct);
            return result ?? new List<BudgetVirementDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching virements: {ex.Message}");
            return new List<BudgetVirementDto>();
        }
    }

    public async Task<BudgetVirementDto> RequestVirementAsync(CreateBudgetVirementRequest request, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/budget/virements", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BudgetVirementDto>(cancellationToken: ct);
        return result!;
    }

    public async Task<List<BudgetCommitmentDto>> GetCommitmentsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<BudgetCommitmentDto>>($"/api/budget/commitments?fiscalYear={fiscalYear}&organisationId={organisationId}", ct);
            return result ?? new List<BudgetCommitmentDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching commitments: {ex.Message}");
            return new List<BudgetCommitmentDto>();
        }
    }

    public async Task<List<BudgetTransactionDto>> GetTransactionsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<BudgetTransactionDto>>($"/api/budget/transactions?fiscalYear={fiscalYear}&organisationId={organisationId}", ct);
            return result ?? new List<BudgetTransactionDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching transactions: {ex.Message}");
            return new List<BudgetTransactionDto>();
        }
    }

    public async Task<List<PayrollBudgetMappingDto>> GetPayrollMappingsAsync(int organisationId = 3, CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<PayrollBudgetMappingDto>>($"/api/budget/payroll-mappings?organisationId={organisationId}", ct);
            return result ?? new List<PayrollBudgetMappingDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching mappings: {ex.Message}");
            return new List<PayrollBudgetMappingDto>();
        }
    }

    public async Task<PayrollBudgetValidationResult> ValidatePayrollRunBudgetAsync(int payrollRunId, int organisationId = 3, CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<PayrollBudgetValidationResult>($"/api/budget/validate-payroll/{payrollRunId}?organisationId={organisationId}", ct);
            return result ?? new PayrollBudgetValidationResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error validating payroll budget: {ex.Message}");
            return new PayrollBudgetValidationResult { Message = ex.Message };
        }
    }

    public async Task<byte[]> ExportBudgetCsvAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        var response = await client.GetAsync($"/api/budget/export/csv?fiscalYear={fiscalYear}&organisationId={organisationId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> GeneratePdfReportAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        var summary = await GetBudgetSummaryAsync(fiscalYear, organisationId, ct);
        return BudgetPdfGenerator.GenerateBudgetExecutionPdf(summary);
    }
}
