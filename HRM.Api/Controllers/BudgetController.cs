using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.DTOs.Budget;
using HRM.Application.Interfaces.Budget;
using HRM.Api.Infrastructure;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Controllers;

[Route("api/[controller]")]
[ApiPermission("Admin")]
public sealed class BudgetController(IBudgetService budgetService, IUserAccessService access) : HrmApiController
{
    private async Task<int> OrganisationAsync(int requested)
    {
        var context = await access.RequireContextAsync();
        var active = context.Organisation?.OrganisationBusinessEntityId ?? context.ActiveJob?.OrganisationId
            ?? await access.GetCurrentOrganisationIdAsync();
        if (active <= 0) throw new UnauthorizedAccessException("An active organisation is required.");
        if (requested <= 0 || requested == active) return active;
        if (context.IsSuperAdministrator) return requested;
        throw new UnauthorizedAccessException("The requested organisation is outside your active organisation scope.");
    }

    [HttpGet("summary")]
    public async Task<ActionResult<BudgetSummaryDto>> GetSummary([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        var summary = await budgetService.GetBudgetSummaryAsync(fiscalYear, await OrganisationAsync(organisationId), ct);
        return Ok(summary);
    }

    [HttpGet("allocations")]
    public async Task<ActionResult<List<BudgetAllocationDto>>> GetAllocations([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        var allocations = await budgetService.GetAllocationsAsync(fiscalYear, await OrganisationAsync(organisationId), ct);
        return Ok(allocations);
    }

    [HttpGet("items")]
    public async Task<ActionResult<List<BudgetItemDto>>> GetItems(CancellationToken ct = default)
    {
        var items = await budgetService.GetBudgetItemsAsync(ct);
        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<ActionResult<BudgetItemDto>> SaveItem([FromBody] CreateBudgetItemRequest request, CancellationToken ct = default)
    {
        var item = await budgetService.SaveBudgetItemAsync(request, ct);
        return Ok(item);
    }

    [HttpGet("fiscal-years")]
    public async Task<ActionResult<List<FiscalYearDto>>> GetFiscalYears(CancellationToken ct = default)
    {
        var list = await budgetService.GetFiscalYearsAsync(ct);
        return Ok(list);
    }

    [HttpPost("fiscal-years")]
    public async Task<ActionResult<FiscalYearDto>> SaveFiscalYear([FromBody] CreateFiscalYearRequest request, CancellationToken ct = default)
    {
        var fy = await budgetService.SaveFiscalYearAsync(request, ct);
        return Ok(fy);
    }

    [HttpPost("payroll-mappings")]
    public async Task<ActionResult<PayrollBudgetMappingDto>> SavePayrollMapping([FromBody] CreatePayrollMappingRequest request, CancellationToken ct = default)
    {
        var orgId = request.CostCenterOrganisationId.HasValue ? await OrganisationAsync(request.CostCenterOrganisationId.Value) : await OrganisationAsync(0);
        var mapping = await budgetService.SavePayrollMappingAsync(request with { CostCenterOrganisationId = orgId }, ct);
        return Ok(mapping);
    }

    [HttpPost("transactions")]
    public async Task<ActionResult<BudgetTransactionDto>> RecordTransaction([FromBody] CreateBudgetTransactionRequest request, CancellationToken ct = default)
    {
        var txn = await budgetService.RecordTransactionAsync(request with { OrganisationId = await OrganisationAsync(request.OrganisationId) }, ct);
        return Ok(txn);
    }

    [HttpPost("allocations")]
    public async Task<ActionResult<BudgetAllocationDto>> SaveAllocation([FromBody] CreateBudgetAllocationRequest request, CancellationToken ct = default)
    {
        var result = await budgetService.SaveAllocationAsync(request with { OrganisationId = await OrganisationAsync(request.OrganisationId) }, ct);
        return Ok(result);
    }

    [HttpGet("virements")]
    public async Task<ActionResult<List<BudgetVirementDto>>> GetVirements([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        var virements = await budgetService.GetVirementsAsync(fiscalYear, await OrganisationAsync(organisationId), ct);
        return Ok(virements);
    }

    [HttpPost("virements")]
    public async Task<ActionResult<BudgetVirementDto>> RequestVirement([FromBody] CreateBudgetVirementRequest request, CancellationToken ct = default)
    {
        var result = await budgetService.RequestVirementAsync(request with { OrganisationId = await OrganisationAsync(request.OrganisationId) }, ct);
        return Ok(result);
    }

    [HttpGet("commitments")]
    public async Task<ActionResult<List<BudgetCommitmentDto>>> GetCommitments([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        var commitments = await budgetService.GetCommitmentsAsync(fiscalYear, await OrganisationAsync(organisationId), ct);
        return Ok(commitments);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<BudgetTransactionDto>>> GetTransactions([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        var transactions = await budgetService.GetTransactionsAsync(fiscalYear, await OrganisationAsync(organisationId), ct);
        return Ok(transactions);
    }

    [HttpGet("payroll-mappings")]
    public async Task<ActionResult<List<PayrollBudgetMappingDto>>> GetPayrollMappings([FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        var mappings = await budgetService.GetPayrollMappingsAsync(await OrganisationAsync(organisationId), ct);
        return Ok(mappings);
    }

    [HttpGet("validate-payroll/{payrollRunId:int}")]
    public async Task<ActionResult<PayrollBudgetValidationResult>> ValidatePayroll(int payrollRunId, [FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        var validation = await budgetService.ValidatePayrollRunBudgetAsync(payrollRunId, await OrganisationAsync(organisationId), ct);
        return Ok(validation);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 0, CancellationToken ct = default)
    {
        organisationId = await OrganisationAsync(organisationId);
        var csvBytes = await budgetService.ExportBudgetCsvAsync(fiscalYear, organisationId, ct);
        return File(csvBytes, "text/csv; charset=utf-8", $"Maldives_State_Budget_Execution_{fiscalYear}_Org{organisationId}.csv");
    }
}
