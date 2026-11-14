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
    public async Task<BudgetSummaryDto> GetBudgetSummaryAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
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

    public async Task<List<BudgetAllocationDto>> GetAllocationsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
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
            if (result != null && result.Count > 0)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching budget items: {ex.Message}");
        }

        // Statutory Default Fallback Items (Maldives Ministry of Finance Standard COA)
        return GetDefaultMofBudgetItems();
    }

    public async Task<BudgetItemDto> SaveBudgetItemAsync(CreateBudgetItemRequest request, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/budget/items", request, ct);
        await EnsureSuccessOrThrowProblemAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<BudgetItemDto>(cancellationToken: ct);
        return result!;
    }

    public async Task<List<FiscalYearDto>> GetFiscalYearsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await client.GetFromJsonAsync<List<FiscalYearDto>>("/api/budget/fiscal-years", ct);
            return result ?? new List<FiscalYearDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error fetching fiscal years: {ex.Message}");
            return new List<FiscalYearDto>
            {
                new() { FiscalYearId = 1, YearNumber = 2026, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), TotalApprovedBudget = 450000000, TotalEmolumentsBudget = 185000000, Status = "Active" }
            };
        }
    }

    public async Task<FiscalYearDto> SaveFiscalYearAsync(CreateFiscalYearRequest request, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/budget/fiscal-years", request, ct);
        await EnsureSuccessOrThrowProblemAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<FiscalYearDto>(cancellationToken: ct);
        return result!;
    }

    public async Task<PayrollBudgetMappingDto> SavePayrollMappingAsync(CreatePayrollMappingRequest request, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/budget/payroll-mappings", request, ct);
        await EnsureSuccessOrThrowProblemAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<PayrollBudgetMappingDto>(cancellationToken: ct);
        return result!;
    }

    public async Task<BudgetTransactionDto> RecordTransactionAsync(CreateBudgetTransactionRequest request, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/budget/transactions", request, ct);
        await EnsureSuccessOrThrowProblemAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<BudgetTransactionDto>(cancellationToken: ct);
        return result!;
    }

    public static List<BudgetItemDto> GetDefaultMofBudgetItems() => new()
    {
        new() { BudgetItemId = 24, EconomicCode = "211001", DhivehiName = "މުވައްޒަފުންގެ މުސާރަ", EnglishName = "Staff Basic Salary", ChapterCode = "21" },
        new() { BudgetItemId = 25, EconomicCode = "211002", DhivehiName = "އިތުރުގަޑީގެ މަސައްކަތަށްދޭ ފައިސާ", EnglishName = "Overtime Pay", ChapterCode = "21" },
        new() { BudgetItemId = 26, EconomicCode = "212001", DhivehiName = "މުވައްޒަފުންނަށް ދޭ ޕޮކެޓްމަނީ", EnglishName = "Staff Pocket Money", ChapterCode = "21" },
        new() { BudgetItemId = 27, EconomicCode = "212002", DhivehiName = "މަތީތަޢުލީމުގެ އެލަވަންސް", EnglishName = "Higher Education Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 28, EconomicCode = "212003", DhivehiName = "ފަންނީ އެލަވަންސް", EnglishName = "Technical / Professional Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 29, EconomicCode = "212004", DhivehiName = "ދިގުމުއްދަތަށް ޚިދުމަތްކުރުމުގެ އެލަވަންސް", EnglishName = "Long Service Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 30, EconomicCode = "212005", DhivehiName = "ރަމަޟާން މަހުގެ މުނާސަބަތުގައި ދޭ އެލަވަންސް", EnglishName = "Ramadan Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 31, EconomicCode = "212006", DhivehiName = "މެޑިކަލް އެލަވަންސް", EnglishName = "Medical Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 39, EconomicCode = "212014", DhivehiName = "ލިވިންގ އެލަވަންސްއާއި ފެމިލީ އެލަވަންސް", EnglishName = "Living & Family Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 44, EconomicCode = "212019", DhivehiName = "ވަގުތީ ހިންގުމުގެ އެލަވަންސް", EnglishName = "Acting / Temporary Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 46, EconomicCode = "212021", DhivehiName = "ޝިފްޓް ޑިއުޓީ އެލަވަންސް", EnglishName = "Shift Duty Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 47, EconomicCode = "212022", DhivehiName = "ހާޑްޝިޕް އެލަވަންސް", EnglishName = "Hardship Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 49, EconomicCode = "212024", DhivehiName = "ފޯނު އެލަވަންސް", EnglishName = "Phone Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 50, EconomicCode = "212025", DhivehiName = "ރިސްކް އެލަވަންސް", EnglishName = "Risk Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 52, EconomicCode = "212027", DhivehiName = "ސަރވިސް އެލަވަންސް", EnglishName = "Service Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 53, EconomicCode = "212028", DhivehiName = "ޒިންމާދާރު ވެރިންނަށް ދެވޭ އެލަވަންސް", EnglishName = "Responsibility Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 54, EconomicCode = "212999", DhivehiName = "އެހެނިހެން ގޮތްގޮތުން މުވައްޒަފުންނަށް ދޭ ފައިސާ", EnglishName = "Other Staff Emoluments", ChapterCode = "21" },
        new() { BudgetItemId = 55, EconomicCode = "213001", DhivehiName = "ޕެންޝަންގެ ފައިސާ", EnglishName = "Pension Allowance", ChapterCode = "21" },
        new() { BudgetItemId = 59, EconomicCode = "213006", DhivehiName = "ރިޓަޔަރމެންޓް ޕެންޝަން ސްކީމަށް ޖަމާކުރާ ފައިސާ", EnglishName = "MRPS Retirement Pension Scheme (7%)", ChapterCode = "21" },
        new() { BudgetItemId = 60, EconomicCode = "221001", DhivehiName = "އެތެރޭގެ ދަތުރުފަތުރު", EnglishName = "Domestic Travel", ChapterCode = "22" },
        new() { BudgetItemId = 61, EconomicCode = "221002", DhivehiName = "ރާއްޖެއިން ބޭރުގެ ދަތުރުފަތުރު", EnglishName = "Overseas Travel", ChapterCode = "22" },
        new() { BudgetItemId = 62, EconomicCode = "222001", DhivehiName = "ސްޓޭޝަނަރީއާއި އޮފީސް ސާމާނު", EnglishName = "Office Stationery & Supplies", ChapterCode = "22" },
        new() { BudgetItemId = 63, EconomicCode = "223001", DhivehiName = "ކަރަންޓު އަގު", EnglishName = "Electricity & Utilities", ChapterCode = "22" },
        new() { BudgetItemId = 64, EconomicCode = "223003", DhivehiName = "ފޯނާއި އިންޓަރނެޓް ޚިދުމަތް", EnglishName = "Telecommunication & Internet", ChapterCode = "22" },
        new() { BudgetItemId = 65, EconomicCode = "225001", DhivehiName = "މުވައްޒަފުން ތަމްރީނުކުރުން", EnglishName = "Staff Training & Development", ChapterCode = "22" },
        new() { BudgetItemId = 70, EconomicCode = "281001", DhivehiName = "އައިޓީ އިކުއިޕްމަންޓާއި ހާޑްވެއަރ", EnglishName = "IT Equipment & Hardware", ChapterCode = "28" },
        new() { BudgetItemId = 71, EconomicCode = "281002", DhivehiName = "ސޮފްޓްވެއަރ އަދި ސިސްޓަމް ލައިސަންސް", EnglishName = "Software & Licences", ChapterCode = "28" },
        new() { BudgetItemId = 72, EconomicCode = "282001", DhivehiName = "އޮފީސް ފަރުނީޗަރާއި ހިފާގެންގުޅޭ ތަކެތި", EnglishName = "Furniture & Fixtures", ChapterCode = "28" }
    };

    public async Task<BudgetAllocationDto> SaveAllocationAsync(CreateBudgetAllocationRequest request, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/budget/allocations", request, ct);
        await EnsureSuccessOrThrowProblemAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<BudgetAllocationDto>(cancellationToken: ct);
        return result!;
    }

    public async Task<List<BudgetVirementDto>> GetVirementsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
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
        await EnsureSuccessOrThrowProblemAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<BudgetVirementDto>(cancellationToken: ct);
        return result!;
    }

    public async Task<List<BudgetCommitmentDto>> GetCommitmentsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
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

    public async Task<List<BudgetTransactionDto>> GetTransactionsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
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

    public async Task<List<PayrollBudgetMappingDto>> GetPayrollMappingsAsync(int organisationId = 0, CancellationToken ct = default)
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

    public async Task<PayrollBudgetValidationResult> ValidatePayrollRunBudgetAsync(int payrollRunId, int organisationId = 0, CancellationToken ct = default)
    {
        try
        {
            using var response = await client.GetAsync($"/api/budget/validate-payroll/{payrollRunId}?organisationId={organisationId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problemMessage = await TryReadProblemTitleAsync(response, ct)
                    ?? $"Budget validation failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
                return new PayrollBudgetValidationResult { Message = problemMessage };
            }

            var result = await response.Content.ReadFromJsonAsync<PayrollBudgetValidationResult>(cancellationToken: ct);
            return result ?? new PayrollBudgetValidationResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BudgetApiClient] Error validating payroll budget: {ex.Message}");
            return new PayrollBudgetValidationResult { Message = ex.Message };
        }
    }

    private static async Task EnsureSuccessOrThrowProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var problemTitle = await TryReadProblemTitleAsync(response, ct);
        if (!string.IsNullOrWhiteSpace(problemTitle))
        {
            throw new InvalidOperationException(problemTitle);
        }
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string?> TryReadProblemTitleAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            if (response.Content is null) return null;
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = title.GetString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = detail.GetString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
        }
        catch
        {
            // Ignore malformed JSON bodies
        }
        return null;
    }

    public async Task<byte[]> ExportBudgetCsvAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        var response = await client.GetAsync($"/api/budget/export/csv?fiscalYear={fiscalYear}&organisationId={organisationId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> GeneratePdfReportAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        var summary = await GetBudgetSummaryAsync(fiscalYear, organisationId, ct);
        return BudgetPdfGenerator.GenerateBudgetExecutionPdf(summary);
    }
}
