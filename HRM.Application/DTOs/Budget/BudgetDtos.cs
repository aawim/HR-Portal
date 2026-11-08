using System;
using System.Collections.Generic;

namespace HRM.Application.DTOs.Budget;

public record BudgetSummaryDto
{
    public int FiscalYear { get; init; } = 2026;
    public string OrganisationName { get; init; } = string.Empty;
    public decimal TotalApprovedBudget { get; init; }
    public decimal TotalRevisedBudget { get; init; }
    public decimal TotalVirementsIn { get; init; }
    public decimal TotalVirementsOut { get; init; }
    public decimal TotalCommitted { get; init; }
    public decimal TotalExpended { get; init; }
    public decimal TotalAvailableBalance { get; init; }
    public decimal OverallBurnRatePercentage { get; init; }
    public List<BudgetAllocationDto> Allocations { get; init; } = new();
}

public record BudgetAllocationDto
{
    public int AllocationId { get; init; }
    public int FiscalYearId { get; init; }
    public int YearNumber { get; init; }
    public int OrganisationId { get; init; }
    public string OrganisationName { get; init; } = string.Empty;
    public int BudgetItemId { get; init; }
    public string EconomicCode { get; init; } = string.Empty;
    public string DhivehiName { get; init; } = string.Empty;
    public string EnglishName { get; init; } = string.Empty;
    public string ChapterCode { get; init; } = "21";
    public decimal ApprovedAmount { get; init; }
    public decimal RevisedAmount { get; init; }
    public decimal VirementInAmount { get; init; }
    public decimal VirementOutAmount { get; init; }
    public decimal NetVirement => VirementInAmount - VirementOutAmount;
    public decimal CurrentBudget => ApprovedAmount + NetVirement;
    public decimal CommittedAmount { get; init; }
    public decimal ExpendedAmount { get; init; }
    public decimal AvailableBalance => CurrentBudget - CommittedAmount - ExpendedAmount;
    public decimal UtilizationPercentage => CurrentBudget > 0 ? Math.Round((ExpendedAmount / CurrentBudget) * 100m, 2) : 0;
    public string? MoFApprovalReference { get; init; }
    public string Status { get; init; } = "Approved";
}

public record CreateBudgetAllocationRequest
{
    public int FiscalYearId { get; init; }
    public int OrganisationId { get; init; }
    public int BudgetItemId { get; init; }
    public decimal ApprovedAmount { get; init; }
    public string? MoFApprovalReference { get; init; }
}

public record BudgetVirementDto
{
    public int VirementId { get; init; }
    public string VirementNumber { get; init; } = string.Empty;
    public int FiscalYearId { get; init; }
    public int YearNumber { get; init; }
    public int OrganisationId { get; init; }
    public int FromBudgetItemId { get; init; }
    public string FromEconomicCode { get; init; } = string.Empty;
    public string FromItemName { get; init; } = string.Empty;
    public int ToBudgetItemId { get; init; }
    public string ToEconomicCode { get; init; } = string.Empty;
    public string ToItemName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Justification { get; init; } = string.Empty;
    public string LegalBasis { get; init; } = string.Empty;
    public string? MoFReferenceNumber { get; init; }
    public DateOnly? MoFApprovedDate { get; init; }
    public string Status { get; init; } = "Approved";
    public DateTime CreatedAtUtc { get; init; }
}

public record CreateBudgetVirementRequest
{
    public int FiscalYearId { get; init; }
    public int OrganisationId { get; init; }
    public int FromBudgetItemId { get; init; }
    public int ToBudgetItemId { get; init; }
    public decimal Amount { get; init; }
    public string Justification { get; init; } = string.Empty;
    public string LegalBasis { get; init; } = string.Empty;
    public string? MoFReferenceNumber { get; init; }
}

public record BudgetCommitmentDto
{
    public int CommitmentId { get; init; }
    public string CommitmentNumber { get; init; } = string.Empty;
    public int FiscalYearId { get; init; }
    public int OrganisationId { get; init; }
    public int BudgetItemId { get; init; }
    public string EconomicCode { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string CommitmentType { get; init; } = "PayrollBatch";
    public int? SourceReferenceId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = "Reserved";
    public DateOnly? ExpiresAt { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public record BudgetTransactionDto
{
    public int BudgetTransactionID { get; init; }
    public int BudgetItemID { get; init; }
    public string EconomicCode { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public int OrganisationID { get; init; }
    public int TransactionTypeID { get; init; }
    public string TransactionTypeName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? BandeyriReferenceNumber { get; init; }
    public string? Details { get; init; }
    public decimal Amount { get; init; }
    public string? SourceDocumentType { get; init; }
    public string Status { get; init; } = "Posted";
}

public record PayrollBudgetMappingDto
{
    public int MappingId { get; init; }
    public int? SalaryComponentId { get; init; }
    public int? AllowanceTypeId { get; init; }
    public int BudgetItemId { get; init; }
    public string EconomicCode { get; init; } = string.Empty;
    public string BudgetItemName { get; init; } = string.Empty;
    public int? CostCenterOrganisationId { get; init; }
    public string ComponentName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public record PayrollBudgetValidationResult
{
    public bool IsSolvent { get; init; }
    public int FiscalYear { get; init; }
    public int OrganisationId { get; init; }
    public decimal TotalRequiredEmoluments { get; init; }
    public decimal TotalAvailableEmoluments { get; init; }
    public List<PayrollBudgetLineCheck> LineChecks { get; init; } = new();
    public string Message { get; init; } = string.Empty;
}

public record PayrollBudgetLineCheck
{
    public int BudgetItemId { get; init; }
    public string EconomicCode { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public decimal RequiredAmount { get; init; }
    public decimal AvailableBalance { get; init; }
    public bool IsSufficient => AvailableBalance >= RequiredAmount;
    public decimal Deficit => IsSufficient ? 0 : RequiredAmount - AvailableBalance;
}

public record BudgetItemDto
{
    public int BudgetItemId { get; init; }
    public string EconomicCode { get; init; } = string.Empty;
    public string DhivehiName { get; init; } = string.Empty;
    public string EnglishName { get; init; } = string.Empty;
    public string ChapterCode { get; init; } = "21";
    public string DisplayText => !string.IsNullOrWhiteSpace(EnglishName) 
        ? $"{EconomicCode} - {DhivehiName} ({EnglishName})" 
        : $"{EconomicCode} - {DhivehiName}";
}

public record CreateBudgetItemRequest
{
    public string EconomicCode { get; init; } = string.Empty;
    public string DhivehiName { get; init; } = string.Empty;
    public string EnglishName { get; init; } = string.Empty;
    public string ChapterCode { get; init; } = "21";
    public string AccountType { get; init; } = "Expenditure";
    public int? ParentBudgetItemId { get; init; }
}

public record FiscalYearDto
{
    public int FiscalYearId { get; init; }
    public int YearNumber { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal TotalApprovedBudget { get; init; }
    public decimal TotalEmolumentsBudget { get; init; }
    public string Status { get; init; } = "Active";
}

public record CreateFiscalYearRequest
{
    public int YearNumber { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal TotalApprovedBudget { get; init; }
    public decimal TotalEmolumentsBudget { get; init; }
}

public record CreatePayrollMappingRequest
{
    public int BudgetItemId { get; init; }
    public string ComponentName { get; init; } = string.Empty;
    public int? SalaryComponentId { get; init; }
    public int? AllowanceTypeId { get; init; }
    public int? CostCenterOrganisationId { get; init; }
}

public record CreateBudgetTransactionRequest
{
    public int BudgetItemId { get; init; }
    public int FiscalYearId { get; init; } = 1;
    public int OrganisationId { get; init; } = 3;
    public int TransactionTypeId { get; init; } = 7; // ActualExpenditure
    public decimal Amount { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string? BandeyriReferenceNumber { get; init; }
    public string Details { get; init; } = string.Empty;
}
