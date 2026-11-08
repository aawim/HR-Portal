using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.DTOs.Budget;
using HRM.Application.Interfaces.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class BudgetController(IBudgetService budgetService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<BudgetSummaryDto>> GetSummary([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var summary = await budgetService.GetBudgetSummaryAsync(fiscalYear, organisationId, ct);
        return Ok(summary);
    }

    [HttpGet("allocations")]
    public async Task<ActionResult<List<BudgetAllocationDto>>> GetAllocations([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var allocations = await budgetService.GetAllocationsAsync(fiscalYear, organisationId, ct);
        return Ok(allocations);
    }

    [HttpGet("items")]
    public async Task<ActionResult<List<BudgetItemDto>>> GetItems(CancellationToken ct = default)
    {
        var items = await budgetService.GetBudgetItemsAsync(ct);
        return Ok(items);
    }

    [HttpPost("allocations")]
    public async Task<ActionResult<BudgetAllocationDto>> SaveAllocation([FromBody] CreateBudgetAllocationRequest request, CancellationToken ct = default)
    {
        var result = await budgetService.SaveAllocationAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("virements")]
    public async Task<ActionResult<List<BudgetVirementDto>>> GetVirements([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var virements = await budgetService.GetVirementsAsync(fiscalYear, organisationId, ct);
        return Ok(virements);
    }

    [HttpPost("virements")]
    public async Task<ActionResult<BudgetVirementDto>> RequestVirement([FromBody] CreateBudgetVirementRequest request, CancellationToken ct = default)
    {
        var result = await budgetService.RequestVirementAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("commitments")]
    public async Task<ActionResult<List<BudgetCommitmentDto>>> GetCommitments([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var commitments = await budgetService.GetCommitmentsAsync(fiscalYear, organisationId, ct);
        return Ok(commitments);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<BudgetTransactionDto>>> GetTransactions([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var transactions = await budgetService.GetTransactionsAsync(fiscalYear, organisationId, ct);
        return Ok(transactions);
    }

    [HttpGet("payroll-mappings")]
    public async Task<ActionResult<List<PayrollBudgetMappingDto>>> GetPayrollMappings([FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var mappings = await budgetService.GetPayrollMappingsAsync(organisationId, ct);
        return Ok(mappings);
    }

    [HttpGet("validate-payroll/{payrollRunId:int}")]
    public async Task<ActionResult<PayrollBudgetValidationResult>> ValidatePayroll(int payrollRunId, [FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var validation = await budgetService.ValidatePayrollRunBudgetAsync(payrollRunId, organisationId, ct);
        return Ok(validation);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] int fiscalYear = 2026, [FromQuery] int organisationId = 3, CancellationToken ct = default)
    {
        var csvBytes = await budgetService.ExportBudgetCsvAsync(fiscalYear, organisationId, ct);
        return File(csvBytes, "text/csv; charset=utf-8", $"Maldives_State_Budget_Execution_{fiscalYear}_Org{organisationId}.csv");
    }
}
