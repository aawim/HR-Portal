using System;
using System.Collections.Generic;

namespace HRM.Models.Budget;

public enum BudgetStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Active = 4,
    Closed = 5,
    Locked = 6
}

public enum BudgetCommitmentStatus
{
    Reserved = 1,
    LiquidatedToExpended = 2,
    Released = 3,
    Cancelled = 4
}

public enum BudgetVirementStatus
{
    Draft = 1,
    PendingInternalApproval = 2,
    MoFSubmitted = 3,
    Approved = 4,
    Rejected = 5
}

public sealed class FiscalYear
{
    public int FiscalYearId { get; set; }
    public int YearNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalApprovedBudget { get; set; }
    public decimal TotalEmolumentsBudget { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<BudgetAllocation> Allocations { get; set; } = new List<BudgetAllocation>();
    public ICollection<BudgetVirement> Virements { get; set; } = new List<BudgetVirement>();
    public ICollection<BudgetCommitment> Commitments { get; set; } = new List<BudgetCommitment>();
}

public sealed class BudgetCategory
{
    public int CategoryId { get; set; }
    public int? ParentCategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameDv { get; set; } = string.Empty;
    public int ChapterNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public BudgetCategory? ParentCategory { get; set; }
    public ICollection<BudgetCategory> ChildCategories { get; set; } = new List<BudgetCategory>();
}

public sealed class BudgetItem
{
    public int BudgetItemID { get; set; }
    public int ItemTypeID { get; set; } = 1;
    public int? Code { get; set; }
    public string? EconomicCode { get; set; }
    public string? MoFClassificationCode { get; set; }
    public int? ParentBudgetItemID { get; set; }
    public string DhivehiName { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public string ChapterCode { get; set; } = "21";
    public string AccountType { get; set; } = "Expenditure";
    public bool IsSalaryComponentLinked { get; set; } = true;
    public bool IsAmountGiven { get; set; } = true;
    public int BudgetYear { get; set; } = 2026;
    public bool IsActive { get; set; } = true;

    public ICollection<BudgetAllocation> Allocations { get; set; } = new List<BudgetAllocation>();
    public ICollection<BudgetTransaction> Transactions { get; set; } = new List<BudgetTransaction>();
    public ICollection<PayrollBudgetMapping> PayrollMappings { get; set; } = new List<PayrollBudgetMapping>();
}

public sealed class BudgetTransactionType
{
    public int Id { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? DhivehiTypeName { get; set; }

    public ICollection<BudgetTransaction> Transactions { get; set; } = new List<BudgetTransaction>();
}

public sealed class BudgetAllocation
{
    public int AllocationId { get; set; }
    public int FiscalYearId { get; set; }
    public int OrganisationId { get; set; }
    public int BudgetItemId { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal RevisedAmount { get; set; }
    public decimal VirementInAmount { get; set; }
    public decimal VirementOutAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ExpendedAmount { get; set; }
    public string? MoFApprovalReference { get; set; }
    public string Status { get; set; } = "Approved";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public FiscalYear? FiscalYear { get; set; }
    public BudgetItem? BudgetItem { get; set; }

    public decimal NetVirement => VirementInAmount - VirementOutAmount;
    public decimal CurrentBudget => ApprovedAmount + NetVirement;
    public decimal AvailableBalance => CurrentBudget - CommittedAmount - ExpendedAmount;
    public decimal UtilizationPercentage => CurrentBudget > 0 ? Math.Round((ExpendedAmount / CurrentBudget) * 100m, 2) : 0;
}

public sealed class BudgetVirement
{
    public int VirementId { get; set; }
    public int FiscalYearId { get; set; }
    public string VirementNumber { get; set; } = string.Empty;
    public int FromBudgetItemId { get; set; }
    public int ToBudgetItemId { get; set; }
    public int OrganisationId { get; set; }
    public decimal Amount { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string? MoFReferenceNumber { get; set; }
    public DateOnly? MoFApprovedDate { get; set; }
    public string Status { get; set; } = "Approved";
    public int? CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public FiscalYear? FiscalYear { get; set; }
    public BudgetItem? FromBudgetItem { get; set; }
    public BudgetItem? ToBudgetItem { get; set; }
}

public sealed class BudgetCommitment
{
    public int CommitmentId { get; set; }
    public int FiscalYearId { get; set; }
    public int OrganisationId { get; set; }
    public int BudgetItemId { get; set; }
    public string CommitmentNumber { get; set; } = string.Empty;
    public string CommitmentType { get; set; } = "PayrollBatch"; // 'PayrollBatch', 'PositionRequisition', 'Procurement'
    public int? SourceReferenceId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Reserved"; // 'Reserved', 'LiquidatedToExpended', 'Released', 'Cancelled'
    public DateOnly? ExpiresAt { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public FiscalYear? FiscalYear { get; set; }
    public BudgetItem? BudgetItem { get; set; }
}

public sealed class BudgetTransaction
{
    public int BudgetTransactionID { get; set; }
    public int BudgetItemID { get; set; }
    public int? FiscalYearID { get; set; }
    public int OrganisationID { get; set; }
    public int TransactionTypeID { get; set; }
    public DateOnly Date { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? LetterNumber { get; set; }
    public string? BandeyriReferenceNumber { get; set; }
    public string? Details { get; set; }
    public decimal Amount { get; set; }
    public string? SourceDocumentType { get; set; }
    public int? SourceDocumentID { get; set; }
    public string Status { get; set; } = "Posted";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BudgetItem? BudgetItem { get; set; }
    public BudgetTransactionType? TransactionType { get; set; }
}

public sealed class PayrollBudgetMapping
{
    public int MappingId { get; set; }
    public int? SalaryComponentId { get; set; }
    public int? AllowanceTypeId { get; set; }
    public int BudgetItemId { get; set; }
    public int? CostCenterOrganisationId { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BudgetItem? BudgetItem { get; set; }
}

public sealed class BudgetAuditEvent
{
    public long AuditEventId { get; set; }
    public int? FiscalYearId { get; set; }
    public int? BudgetItemId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? PreviousValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public int? PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
