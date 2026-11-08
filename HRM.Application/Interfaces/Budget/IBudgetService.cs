using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.DTOs.Budget;

namespace HRM.Application.Interfaces.Budget;

public interface IBudgetService
{
    Task<BudgetSummaryDto> GetBudgetSummaryAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default);
    Task<List<BudgetAllocationDto>> GetAllocationsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default);
    Task<BudgetAllocationDto> SaveAllocationAsync(CreateBudgetAllocationRequest request, CancellationToken ct = default);
    Task<List<BudgetVirementDto>> GetVirementsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default);
    Task<BudgetVirementDto> RequestVirementAsync(CreateBudgetVirementRequest request, CancellationToken ct = default);
    Task<List<BudgetCommitmentDto>> GetCommitmentsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default);
    Task<List<BudgetTransactionDto>> GetTransactionsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default);
    Task<List<PayrollBudgetMappingDto>> GetPayrollMappingsAsync(int organisationId = 3, CancellationToken ct = default);
    Task<PayrollBudgetValidationResult> ValidatePayrollRunBudgetAsync(int payrollRunId, int organisationId = 3, CancellationToken ct = default);
    Task<byte[]> ExportBudgetCsvAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default);
}
