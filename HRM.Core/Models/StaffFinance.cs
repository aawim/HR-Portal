namespace HRM.Models;

public sealed class FinanceCurrency
{
    public int CurrencyTypeId { get; set; }
    public string Name { get; set; } = "";
}

public sealed class AllowanceType
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public sealed class PositionAllowanceRule
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public int PositionId { get; set; }
    public long AllowanceTypeId { get; set; }
    public int CurrencyTypeId { get; set; }
    public decimal Amount { get; set; }
    public int FrequencyMonths { get; set; } = 1;
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string MultiplePositionPolicy { get; set; } = "Once";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
public sealed class StaffAllowance
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int RequestId { get; set; }
    public long AllowanceTypeId { get; set; }
    public int CurrencyTypeId { get; set; }
    public decimal Amount { get; set; }
    public int FrequencyMonths { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
public sealed class AllowanceAssessment
{
    public long Id {get;set;}
    public int RequestId {get;set;}
    public int RequestStateId {get;set;}
    public decimal Amount {get;set;}
    public int CurrencyTypeId {get;set;}
    public int AssessedByUserId {get;set;}
    public DateTime AssessedAtUtc {get;set;}
}
public sealed class DeductionPolicy
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public int CurrencyTypeId { get; set; }
    public decimal MaximumPerMonth { get; set; }
    public decimal MaximumPercentOfBasic { get; set; }
    public decimal MinimumTakeHome { get; set; }
    public decimal MaximumOutstanding { get; set; }
    public int MaximumAgreements { get; set; }
    public DateTime FromDate { get; set; }
    public int CreatedByUserId { get; set; }
}
public sealed class DeductionAgreement
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int RequestId { get; set; }
    public int CreditorBusinessEntityId { get; set; }
    public string Reference { get; set; } = "";
    public string Category { get; set; } = "Loan";
    public int CurrencyTypeId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime FirstDueDate { get; set; }
    public int InstalmentCount { get; set; }
    public int FrequencyMonths { get; set; }
    public int ScheduleVersion { get; set; } = 1;
}
public sealed class DeductionInstalment
{
    public long Id { get; set; }
    public long AgreementId { get; set; }
    public int Version { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsSuperseded { get; set; }
    public int SourceRequestId { get; set; }
}
public sealed class DeductionPayment
{
    public long Id { get; set; }
    public long AgreementId { get; set; }
    public int? RequestId { get; set; }
    public long? PayrollEntryId { get; set; }
    public int? PayerBusinessEntityId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; }
    public string Reference { get; set; } = "";
    public int RecordedByUserId { get; set; }
}
public sealed class DeductionPaymentAllocation
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public long InstalmentId { get; set; }
    public decimal Amount { get; set; }
}
public sealed class StaffFinanceRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public string Kind { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public Guid ClientRequestId { get; set; }
    public int CreatedByUserId { get; set; }
    public Request Request { get; set; } = null!;
}
public sealed class StaffFinanceDocument
{
    public long Id { get; set; }
    public int RequestId { get; set; }
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = [];
    public int UploadedByUserId { get; set; }
}
public sealed class StaffFinancePayrollBatch
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public DateTime Month { get; set; }
    public int PayrollCycleRequestId { get; set; }
    public Guid ClientBatchId { get; set; }
    public string Reference { get; set; } = "";
    public int PostedByUserId { get; set; }
    public DateTime PostedAtUtc { get; set; }
    public string SelectionJson { get; set; } = "";
    public string Fingerprint { get; set; } = "";
}
public sealed class StaffFinancePayrollEntry
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public DateTime Month { get; set; }
    public string SourceKey { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Description { get; set; } = "";
    public int CurrencyTypeId { get; set; }
    public decimal Amount { get; set; }
    public long? InstalmentId { get; set; }
    public long? AllowanceId { get; set; }
    public long? PositionRuleId { get; set; }
    public int? JobPositionId { get; set; }
}
