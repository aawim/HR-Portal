using System;
using System.Collections.Generic;

namespace HRM.Models.Payroll;

public sealed class PayrollPeriod
{
    public int PayrollPeriodId { get; set; }
    public int OrganisationId { get; set; }
    public int? PayrollGroupId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public DateOnly AttendanceCutoffDate { get; set; }
    public DateOnly AdjustmentCutoffDate { get; set; }
    public DateOnly PaymentDate { get; set; }
    public PayrollPeriodStatus PeriodStatus { get; set; } = PayrollPeriodStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? OperationLogId { get; set; }

    public ICollection<PayrollRun> PayrollRuns { get; set; } = new List<PayrollRun>();
}

public sealed class SalaryComponent
{
    public int SalaryComponentId { get; set; }
    public int OrganisationId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public SalaryComponentType ComponentType { get; set; }
    public CalculationMethodType CalculationMethod { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool IsTaxable { get; set; } = true;
    public bool IsPensionable { get; set; } = true;
    public bool IsProratable { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal DefaultAmount { get; set; }
    public decimal DefaultPercentage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SalaryStructure
{
    public int SalaryStructureId { get; set; }
    public int OrganisationId { get; set; }
    public string StructureCode { get; set; } = string.Empty;
    public string StructureName { get; set; } = string.Empty;
    public string? GradeCode { get; set; }
    public string? Description { get; set; }
    public decimal MinimumSalary { get; set; }
    public decimal MaximumSalary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmployeeCompensation
{
    public int EmployeeCompensationId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public int? SalaryStructureId { get; set; }
    public decimal BasicSalary { get; set; }
    public string? GradeCode { get; set; }
    public int? StepNumber { get; set; }
    public int CurrencyTypeId { get; set; } = 1;
    public string BankName { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
}

public sealed class CalculationRule
{
    public int CalculationRuleId { get; set; }
    public int OrganisationId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty; // "Pension", "Tax", "Overtime", "LeaveProration"
    public decimal PercentageValue { get; set; }
    public decimal FlatThresholdValue { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConfigurationJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PayrollRun
{
    public int PayrollRunId { get; set; }
    public int OrganisationId { get; set; }
    public int PayrollPeriodId { get; set; }
    public PayrollRunType RunType { get; set; } = PayrollRunType.RegularMonthly;
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalEmployees { get; set; }
    public int ProcessedEmployees { get; set; }
    public decimal TotalGrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalEmployerContributions { get; set; }
    public decimal TotalNetPayable { get; set; }
    public int? PreparedByUserId { get; set; }
    public DateTime? PreparedAtUtc { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public int? FinalisedByUserId { get; set; }
    public DateTime? FinalisedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public PayrollPeriod? PayrollPeriod { get; set; }
    public ICollection<PayrollRunBatch> Batches { get; set; } = new List<PayrollRunBatch>();
    public ICollection<PayrollEmployee> Employees { get; set; } = new List<PayrollEmployee>();
    public ICollection<PayrollException> Exceptions { get; set; } = new List<PayrollException>();
}

public sealed class PayrollRunBatch
{
    public int PayrollRunBatchId { get; set; }
    public int PayrollRunId { get; set; }
    public int BatchNumber { get; set; }
    public int TotalEmployees { get; set; }
    public PayrollBatchStatus Status { get; set; } = PayrollBatchStatus.Pending;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public PayrollRun? PayrollRun { get; set; }
}

public sealed class PayrollEmployee
{
    public int PayrollEmployeeId { get; set; }
    public int PayrollRunId { get; set; }
    public int? PayrollRunBatchId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public string? GradeCode { get; set; }
    public int? StepNumber { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal EmployerContributions { get; set; }
    public decimal NetSalary { get; set; }
    public string CalculationStatus { get; set; } = "Calculated"; // "Calculated", "Warning", "Error"
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public PayrollRun? PayrollRun { get; set; }
    public ICollection<PayrollLine> Lines { get; set; } = new List<PayrollLine>();
}

public sealed class PayrollLine
{
    public int PayrollLineId { get; set; }
    public int PayrollEmployeeId { get; set; }
    public int PayrollRunId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public SalaryComponentType ComponentType { get; set; }
    public decimal Rate { get; set; }
    public decimal Units { get; set; } = 1.0m;
    public decimal CalculatedAmount { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsPensionable { get; set; }
    public string? SourceReference { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public PayrollEmployee? PayrollEmployee { get; set; }
}

public sealed class PayrollInputSnapshot
{
    public int PayrollInputSnapshotId { get; set; }
    public int PayrollRunId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public int SnapshotVersion { get; set; } = 1;
    public string SnapshotHash { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PayrollException
{
    public int PayrollExceptionId { get; set; }
    public int PayrollRunId { get; set; }
    public int? JobId { get; set; }
    public int? IndividualId { get; set; }
    public PayrollIssueSeverity Severity { get; set; } = PayrollIssueSeverity.Warning;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool IsAcknowledged { get; set; }
    public int? AcknowledgedByUserId { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public PayrollRun? PayrollRun { get; set; }
}

public sealed class PayrollAdjustment
{
    public int PayrollAdjustmentId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public int? PayrollRunId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsAddition { get; set; } = true;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PaymentBatch
{
    public int PaymentBatchId { get; set; }
    public int OrganisationId { get; set; }
    public int PayrollRunId { get; set; }
    public string BatchReference { get; set; } = string.Empty;
    public string BankCode { get; set; } = "BML";
    public string FormatCode { get; set; } = "CSV";
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
    public PaymentBatchStatus Status { get; set; } = PaymentBatchStatus.Draft;
    public string? ExportedContent { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }

    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}

public sealed class PaymentTransaction
{
    public int PaymentTransactionId { get; set; }
    public int PaymentBatchId { get; set; }
    public int PayrollEmployeeId { get; set; }
    public int IndividualId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CurrencyTypeId { get; set; } = 1;
    public string PaymentReference { get; set; } = string.Empty;
    public string Status { get; set; } = "Prepared"; // "Prepared", "Transferred", "Failed", "Reconciled"
    public string? TransactionReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    public PaymentBatch? PaymentBatch { get; set; }
}

public sealed class PayrollAuditEvent
{
    public int PayrollAuditEventId { get; set; }
    public int OrganisationId { get; set; }
    public int? PayrollRunId { get; set; }
    public int UserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? OldState { get; set; }
    public string? NewState { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}

public sealed class PayrollBackgroundJob
{
    public long JobId { get; set; }
    public int? PayrollRunId { get; set; }
    public int OrganisationId { get; set; }
    public PayrollJobType JobType { get; set; }
    public PayrollJobStatus Status { get; set; } = PayrollJobStatus.Pending;
    public int Priority { get; set; } = 100;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public long FencingToken { get; set; }
    public DateTime? LastHeartbeatAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PayloadJson { get; set; }
}
