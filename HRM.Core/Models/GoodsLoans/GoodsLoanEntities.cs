using System;
using System.Collections.Generic;

namespace HRM.Models.GoodsLoans;

public static class GoodsLoanConstants
{
    public const int GoodsLoanRequestTypeId = 130;
    public const string LoanCategory = "GoodsLoan";
    public const string PayrollComponentCode = "GL_DED";
    public const string PayrollComponentName = "Goods Loan Deduction";
}

public static class GoodsLoanStatus
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Approved = "APPROVED";
    public const string Active = "ACTIVE";
    public const string Completed = "COMPLETED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";
    public const string Defaulted = "DEFAULTED";
}

public static class GoodsLoanInstallmentStatus
{
    public const string Pending = "PENDING";
    public const string PartiallyPaid = "PARTIALLY_PAID";
    public const string Paid = "PAID";
    public const string Cancelled = "CANCELLED";
}

public static class GoodsLoanDeductionTransactionType
{
    public const string Deduction = "DEDUCTION";
    public const string Reversal = "REVERSAL";
}

public static class GoodsLoanDeductionStatus
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Voided = "VOIDED";
}

public static class GoodsLoanSettlementMode
{
    public const string Monthly = "MONTHLY";
    public const string Upfront = "UPFRONT";
}

public static class GoodsLoanSettlementStatus
{
    public const string Draft = "DRAFT";
    public const string Approved = "APPROVED";
    public const string Paid = "PAID";
    public const string Cancelled = "CANCELLED";
}

public static class SalaryCalculationBasis
{
    public const string BasicSalary = "BasicSalary";
    public const string GrossSalary = "GrossSalary";
    public const string EligibleSalaryComponents = "EligibleSalaryComponents";
}

public sealed class GoodsLoanStore
{
    public int StoreID { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int CreatedByUserID { get; set; }

    public ICollection<GoodsLoanStoreAgreement> Agreements { get; set; } = new List<GoodsLoanStoreAgreement>();
    public ICollection<GoodsLoan> Loans { get; set; } = new List<GoodsLoan>();
    public ICollection<GoodsLoanStoreSettlement> Settlements { get; set; } = new List<GoodsLoanStoreSettlement>();
}

public sealed class GoodsLoanStoreAgreement
{
    public int AgreementID { get; set; }
    public int StoreID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int MinimumMonths { get; set; } = 1;
    public int MaximumMonths { get; set; } = 36;
    public decimal MaximumLoanAmount { get; set; }
    public decimal InterestRate { get; set; } = 0.0m;
    public string SettlementMode { get; set; } = GoodsLoanSettlementMode.Monthly;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public GoodsLoanStore? Store { get; set; }
    public ICollection<GoodsLoan> Loans { get; set; } = new List<GoodsLoan>();
}

public sealed class GoodsLoanDeductionPolicy
{
    public int PolicyID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public decimal MaximumSalaryPercentage { get; set; } = 20.0m; // 20%
    public decimal MinimumNetSalary { get; set; } = 0.0m;
    public int? MaximumActiveLoans { get; set; }
    public decimal? MaximumOutstandingAmount { get; set; }
    public string SalaryBasis { get; set; } = SalaryCalculationBasis.BasicSalary;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<GoodsLoan> Loans { get; set; } = new List<GoodsLoan>();
}

public sealed class GoodsLoan
{
    public int LoanID { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public int OrganisationBusinessEntityID { get; set; }
    public int IndividualID { get; set; }
    public int JobID { get; set; }
    public int StoreID { get; set; }
    public int AgreementID { get; set; }
    public int PolicyID { get; set; }
    public int? RequestID { get; set; }
    public int? WorkflowRequestInstanceID { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal TotalRepayableAmount { get; set; }
    public int RepaymentMonths { get; set; }
    public DateTime FirstDeductionMonth { get; set; }
    public decimal SalaryAtApproval { get; set; }
    public decimal EligibleSalaryAtApproval { get; set; }
    public decimal DeductionPercentageAtApproval { get; set; }
    public string Status { get; set; } = GoodsLoanStatus.Draft;
    public int? ApprovedByUserID { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string? Remarks { get; set; }
    public int CreatedByUserID { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public GoodsLoanStore? Store { get; set; }
    public GoodsLoanStoreAgreement? Agreement { get; set; }
    public GoodsLoanDeductionPolicy? Policy { get; set; }
    public Request? Request { get; set; }

    public ICollection<GoodsLoanItem> Items { get; set; } = new List<GoodsLoanItem>();
    public ICollection<GoodsLoanInstallment> Installments { get; set; } = new List<GoodsLoanInstallment>();
    public ICollection<GoodsLoanStatusHistory> StatusHistory { get; set; } = new List<GoodsLoanStatusHistory>();
    public ICollection<GoodsLoanStoreSettlementAllocation> SettlementAllocations { get; set; } = new List<GoodsLoanStoreSettlementAllocation>();
    public ICollection<GoodsLoanDocument> Documents { get; set; } = new List<GoodsLoanDocument>();
}

public sealed class GoodsLoanItem
{
    public int LoanItemID { get; set; }
    public int LoanID { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }

    public GoodsLoan? Loan { get; set; }
}

public sealed class GoodsLoanInstallment
{
    public int InstallmentID { get; set; }
    public int LoanID { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueMonth { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0.0m;
    public string Status { get; set; } = GoodsLoanInstallmentStatus.Pending;
    public DateTime? LastPaymentDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public decimal OutstandingAmount => Math.Max(0m, ScheduledAmount - PaidAmount);

    public GoodsLoan? Loan { get; set; }
    public ICollection<GoodsLoanPayrollDeduction> Deductions { get; set; } = new List<GoodsLoanPayrollDeduction>();
}

public sealed class GoodsLoanPayrollDeduction
{
    public int DeductionID { get; set; }
    public int InstallmentID { get; set; }
    public int PayrollRunID { get; set; }
    public int? PayrollItemID { get; set; }
    public string TransactionType { get; set; } = GoodsLoanDeductionTransactionType.Deduction;
    public decimal Amount { get; set; }
    public string Status { get; set; } = GoodsLoanDeductionStatus.Pending;
    public DateTime DeductionDate { get; set; }
    public int? ReversesDeductionID { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public GoodsLoanInstallment? Installment { get; set; }
}

public sealed class GoodsLoanStoreSettlement
{
    public int SettlementID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public int StoreID { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = GoodsLoanSettlementStatus.Draft;
    public int CreatedByUserID { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public GoodsLoanStore? Store { get; set; }
    public ICollection<GoodsLoanStoreSettlementAllocation> Allocations { get; set; } = new List<GoodsLoanStoreSettlementAllocation>();
}

public sealed class GoodsLoanStoreSettlementAllocation
{
    public int AllocationID { get; set; }
    public int SettlementID { get; set; }
    public int LoanID { get; set; }
    public decimal AllocatedAmount { get; set; }

    public GoodsLoanStoreSettlement? Settlement { get; set; }
    public GoodsLoan? Loan { get; set; }
}

public sealed class GoodsLoanStatusHistory
{
    public int HistoryID { get; set; }
    public int LoanID { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int ChangedByUserID { get; set; }
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;

    public GoodsLoan? Loan { get; set; }
}

public sealed class GoodsLoanDocument
{
    public int DocumentID { get; set; }
    public int LoanID { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public int UploadedByUserID { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public GoodsLoan? Loan { get; set; }
}
