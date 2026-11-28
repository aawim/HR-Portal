using System;
using System.Collections.Generic;

namespace HRM.DTOs.GoodsLoans;

public sealed class GoodsLoanStoreDto
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
    public DateTime CreatedDate { get; set; }
    public int ActiveAgreementsCount { get; set; }
    public int ActiveLoansCount { get; set; }
}

public sealed class SaveGoodsLoanStoreRequest
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
}

public sealed class GoodsLoanStoreAgreementDto
{
    public int AgreementID { get; set; }
    public int StoreID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public int OrganisationBusinessEntityID { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string AgreementNumber { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int MinimumMonths { get; set; } = 1;
    public int MaximumMonths { get; set; } = 36;
    public decimal MaximumLoanAmount { get; set; }
    public decimal InterestRate { get; set; } = 0.0m;
    public string SettlementMode { get; set; } = "MONTHLY";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
}

public sealed class SaveGoodsLoanStoreAgreementRequest
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
    public string SettlementMode { get; set; } = "MONTHLY";
    public bool IsActive { get; set; } = true;
}

public sealed class GoodsLoanDeductionPolicyDto
{
    public int PolicyID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public decimal MaximumSalaryPercentage { get; set; }
    public decimal MinimumNetSalary { get; set; }
    public int? MaximumActiveLoans { get; set; }
    public decimal? MaximumOutstandingAmount { get; set; }
    public string SalaryBasis { get; set; } = "BasicSalary";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaveGoodsLoanDeductionPolicyRequest
{
    public int PolicyID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public decimal MaximumSalaryPercentage { get; set; } = 20.0m;
    public decimal MinimumNetSalary { get; set; } = 0.0m;
    public int? MaximumActiveLoans { get; set; }
    public decimal? MaximumOutstandingAmount { get; set; }
    public string SalaryBasis { get; set; } = "BasicSalary";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class GoodsLoanItemDto
{
    public int LoanItemID { get; set; }
    public int LoanID { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class GoodsLoanInstallmentDto
{
    public int InstallmentID { get; set; }
    public int LoanID { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueMonth { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount => Math.Max(0m, ScheduledAmount - PaidAmount);
    public string Status { get; set; } = "PENDING";
    public DateTime? LastPaymentDate { get; set; }
    public bool IsOverdue => Status != "PAID" && Status != "CANCELLED" && DueMonth < DateTime.Today && OutstandingAmount > 0;
}

public sealed class GoodsLoanPayrollDeductionDto
{
    public int DeductionID { get; set; }
    public int InstallmentID { get; set; }
    public int PayrollRunID { get; set; }
    public int? PayrollItemID { get; set; }
    public string TransactionType { get; set; } = "DEDUCTION";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime DeductionDate { get; set; }
    public int? ReversesDeductionID { get; set; }
    public DateTime CreatedDate { get; set; }
}

public sealed class GoodsLoanStatusHistoryDto
{
    public int HistoryID { get; set; }
    public int LoanID { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int ChangedByUserID { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; }
}

public sealed class GoodsLoanDocumentDto
{
    public int DocumentID { get; set; }
    public int LoanID { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
}

public sealed class GoodsLoanDto
{
    public int LoanID { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public int OrganisationBusinessEntityID { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public int IndividualID { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public int JobID { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int StoreID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int AgreementID { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string SettlementMode { get; set; } = "MONTHLY";
    public int PolicyID { get; set; }
    public int? RequestID { get; set; }
    public int? WorkflowRequestInstanceID { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal TotalRepayableAmount { get; set; }
    public int RepaymentMonths { get; set; }
    public DateTime FirstDeductionMonth { get; set; }
    public decimal MonthlyInstalmentAmount { get; set; }
    public decimal MonthlyDeductionAmount { get => MonthlyInstalmentAmount; set => MonthlyInstalmentAmount = value; }
    public decimal TotalPaidAmount { get; set; }
    public decimal PaidAmount { get => TotalPaidAmount; set => TotalPaidAmount = value; }
    public decimal OutstandingBalance => Math.Max(0m, TotalRepayableAmount - TotalPaidAmount);
    public decimal OutstandingAmount => OutstandingBalance;
    public string EmployeeName { get => StaffName; set => StaffName = value; }
    public int PaidInstallmentsCount => Installments?.Count(i => i.Status == "PAID") ?? 0;
    public decimal SalaryAtApproval { get; set; }
    public decimal EligibleSalaryAtApproval { get; set; }
    public decimal DeductionPercentageAtApproval { get; set; }
    public string Status { get; set; } = "DRAFT";
    public int? ApprovedByUserID { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }

    public List<GoodsLoanItemDto> Items { get; set; } = [];
    public List<GoodsLoanInstallmentDto> Installments { get; set; } = [];
    public List<GoodsLoanPayrollDeductionDto> PayrollDeductions { get; set; } = [];
    public List<GoodsLoanStatusHistoryDto> StatusHistory { get; set; } = [];
    public List<GoodsLoanDocumentDto> Documents { get; set; } = [];
}

public sealed class CreateGoodsLoanApplicationRequest
{
    public int IndividualID { get; set; }
    public int JobID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public int StoreID { get; set; }
    public int AgreementID { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal TotalRepayableAmount { get; set; }
    public int RepaymentMonths { get; set; }
    public DateTime FirstDeductionMonth { get; set; }
    public string? Remarks { get; set; }
    public List<GoodsLoanItemDto> Items { get; set; } = [];
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public byte[]? AttachmentBytes { get; set; }
}

public sealed class GoodsLoanEligibilityCheckRequest
{
    public int IndividualID { get; set; }
    public int JobID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public int AgreementID { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal PrincipalAmount { get => RequestedAmount; set => RequestedAmount = value; }
    public int RequestedMonths { get; set; }
    public int RepaymentMonths { get => RequestedMonths; set => RequestedMonths = value; }
    public DateTime FirstDeductionMonth { get; set; }
}

public sealed class GoodsLoanEligibilityResult
{
    public bool IsEligible { get; set; }
    public decimal EmployeeSalary { get; set; }
    public string SalaryBasis { get; set; } = "BasicSalary";
    public decimal MaximumAllowedPercentage { get; set; }
    public decimal MaximumAllowedMonthlyDeduction { get; set; }
    public decimal CurrentMonthlyGoodsLoanDeductions { get; set; }
    public decimal CurrentOtherMonthlyDeductions { get; set; }
    public decimal ProposedNewMonthlyDeduction { get; set; }
    public decimal AvailableMonthlyCapacity => Math.Max(0m, MaximumAllowedMonthlyDeduction - CurrentMonthlyGoodsLoanDeductions);
    public decimal MinimumTakeHomeSalary { get; set; }
    public decimal ProjectedNetSalary { get; set; }
    public int CurrentActiveLoansCount { get; set; }
    public int? MaximumAllowedActiveLoans { get; set; }
    public decimal CurrentTotalOutstanding { get; set; }
    public decimal? MaximumAllowedOutstanding { get; set; }
    public List<string> ValidationErrors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<MonthlyCommitmentDetail> FutureMonthsBreakdown { get; set; } = [];
}

public sealed class MonthlyCommitmentDetail
{
    public DateTime Month { get; set; }
    public decimal ExistingDeductions { get; set; }
    public decimal ProposedDeduction { get; set; }
    public decimal TotalDeduction => ExistingDeductions + ProposedDeduction;
    public decimal CapacityLimit { get; set; }
    public bool ExceedsCapacity => TotalDeduction > CapacityLimit;
}

public sealed class RepaymentScheduleCalculationRequest
{
    public decimal TotalAmount { get; set; }
    public int RepaymentMonths { get; set; }
    public DateTime FirstDeductionMonth { get; set; }
}

public sealed class RepaymentScheduleInstalmentPlan
{
    public int InstalmentNumber { get; set; }
    public int InstallmentNumber { get => InstalmentNumber; set => InstalmentNumber = value; }
    public DateTime DueMonth { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal RemainingPrincipalAfter { get; set; }
}

public sealed class GoodsLoanStoreSettlementDto
{
    public int SettlementID { get; set; }
    public int OrganisationBusinessEntityID { get; set; }
    public int StoreID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string SettlementNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "DRAFT";
    public DateTime CreatedDate { get; set; }
    public List<GoodsLoanStoreSettlementAllocationDto> Allocations { get; set; } = [];
}

public sealed class GoodsLoanStoreSettlementAllocationDto
{
    public int AllocationID { get; set; }
    public int SettlementID { get; set; }
    public int LoanID { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
}

public sealed class CreateStoreSettlementBatchRequest
{
    public int OrganisationBusinessEntityID { get; set; }
    public int StoreID { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public DateTime SettlementDate { get => PaymentDate; set => PaymentDate = value; }
    public string PaymentReference { get; set; } = string.Empty;
    public List<int> LoanIds { get; set; } = [];
}

public sealed class GoodsLoanStaffWorkspaceSummary
{
    public int ActiveLoansCount { get; set; }
    public decimal TotalOutstandingBalance { get; set; }
    public decimal CurrentMonthlyDeduction { get; set; }
    public decimal AvailableMonthlyCapacity { get; set; }
    public decimal MaximumAllowedDeduction { get; set; }
    public DateTime? NextInstalmentDate { get; set; }
    public decimal? NextInstalmentAmount { get; set; }
    public int CompletedLoansCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public List<GoodsLoanDto> ActiveLoans { get; set; } = [];
    public List<GoodsLoanDto> AllLoans { get; set; } = [];
}

public sealed class GoodsLoanFilterRequest
{
    public int? OrganisationBusinessEntityID { get; set; }
    public int? IndividualID { get; set; }
    public int? JobID { get; set; }
    public int? StoreID { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool OnlyOutstanding { get; set; }
}

public sealed class EarlyLoanSettlementRequest
{
    public int LoanID { get; set; }
    public decimal SettlementAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string message = "") => new() { Success = true, Data = data, Message = message };
    public static ServiceResult<T> Fail(string message) => new() { Success = false, Message = message };
}
