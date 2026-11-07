using System;
using System.Collections.Generic;
using HRM.Models.Payroll;

namespace HRM.Application.Features.Payroll;

public sealed record PayrollPeriodDto(
    int PayrollPeriodId,
    int OrganisationId,
    int? PayrollGroupId,
    int PeriodYear,
    int PeriodMonth,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    DateOnly AttendanceCutoffDate,
    DateOnly AdjustmentCutoffDate,
    DateOnly PaymentDate,
    PayrollPeriodStatus PeriodStatus,
    int TotalRunsCount
);

public sealed record PayrollRunDto(
    int PayrollRunId,
    int OrganisationId,
    int PayrollPeriodId,
    PayrollRunType RunType,
    PayrollRunStatus Status,
    string Title,
    string? Description,
    int TotalEmployees,
    int ProcessedEmployees,
    decimal TotalGrossEarnings,
    decimal TotalDeductions,
    decimal TotalEmployerContributions,
    decimal TotalNetPayable,
    int? PreparedByUserId,
    DateTime? PreparedAtUtc,
    int? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    int? FinalisedByUserId,
    DateTime? FinalisedAtUtc,
    int ExceptionCount,
    int BlockingExceptionCount
);

public sealed record PayrollEmployeeSummaryDto(
    int PayrollEmployeeId,
    int PayrollRunId,
    int? PayrollRunBatchId,
    int JobId,
    int IndividualId,
    string EmployeeNumber,
    string EmployeeName,
    string DepartmentName,
    string PositionTitle,
    string? GradeCode,
    int? StepNumber,
    decimal BaseSalary,
    decimal GrossEarnings,
    decimal TotalDeductions,
    decimal EmployerContributions,
    decimal NetSalary,
    string CalculationStatus,
    string? ErrorMessage,
    List<PayrollLineDto> Lines
);

public sealed record PayrollLineDto(
    int PayrollLineId,
    int PayrollEmployeeId,
    string ComponentCode,
    string ComponentName,
    SalaryComponentType ComponentType,
    decimal Rate,
    decimal Units,
    decimal CalculatedAmount,
    bool IsTaxable,
    bool IsPensionable,
    string? SourceReference
);

public sealed record PayrollProgressDto(
    int PayrollRunId,
    PayrollRunStatus Status,
    int TotalEmployees,
    int ProcessedEmployees,
    int SuccessfulEmployees,
    int FailedEmployees,
    double PercentComplete,
    string CurrentStage,
    List<string> RecentErrors
);

public sealed record SalaryComponentDto(
    int SalaryComponentId,
    int OrganisationId,
    string ComponentCode,
    string ComponentName,
    SalaryComponentType ComponentType,
    CalculationMethodType CalculationMethod,
    bool IsRecurring,
    bool IsTaxable,
    bool IsPensionable,
    bool IsProratable,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    decimal DefaultAmount,
    decimal DefaultPercentage
);

public sealed record EmployeeCompensationDto(
    int EmployeeCompensationId,
    int OrganisationId,
    int JobId,
    int IndividualId,
    string EmployeeName,
    decimal BasicSalary,
    string? GradeCode,
    int? StepNumber,
    int CurrencyTypeId,
    string BankName,
    string BankBranch,
    string BankAccountNumber,
    string AccountHolderName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive
);

public sealed record PaymentBatchDto(
    int PaymentBatchId,
    int OrganisationId,
    int PayrollRunId,
    string BatchReference,
    string BankCode,
    string FormatCode,
    decimal TotalAmount,
    int TotalCount,
    PaymentBatchStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc
);

public sealed record PayslipDto(
    int PayrollEmployeeId,
    int PayrollRunId,
    int OrganisationId,
    string OrganisationName,
    int IndividualId,
    string EmployeeNumber,
    string EmployeeName,
    string DepartmentName,
    string PositionTitle,
    string? GradeCode,
    int? StepNumber,
    int PeriodYear,
    int PeriodMonth,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    DateOnly PaymentDate,
    decimal BaseSalary,
    decimal GrossEarnings,
    decimal TotalDeductions,
    decimal EmployerContributions,
    decimal NetSalary,
    string BankName,
    string BankAccountNumber,
    string PaymentReference,
    List<PayrollLineDto> Earnings,
    List<PayrollLineDto> Deductions,
    List<PayrollLineDto> EmployerItems
);

public sealed record EmployeePayrollInputItem(
    int JobId,
    int IndividualId,
    string EmployeeNumber,
    string EmployeeName,
    string DepartmentName,
    string PositionTitle,
    string? GradeCode,
    int? StepNumber,
    decimal BasicSalary,
    int CurrencyTypeId,
    string BankName,
    string BankBranch,
    string BankAccountNumber,
    string AccountHolderName,
    DateOnly JoinedDate,
    DateOnly? TerminatedDate,
    List<InputAllowanceItem> Allowances,
    List<InputOvertimeItem> OvertimeClaims,
    List<InputLeaveDeductionItem> UnpaidLeaves,
    List<InputDeductionAgreementItem> Deductions,
    List<InputAdjustmentItem> Adjustments
);

public sealed record InputAllowanceItem(
    long AllowanceId,
    string ComponentCode,
    string Name,
    SalaryComponentType Type,
    decimal Amount,
    bool IsTaxable,
    bool IsPensionable,
    bool IsProratable,
    DateOnly FromDate,
    DateOnly? ToDate
);

public sealed record InputOvertimeItem(
    int RequestId,
    string RequestReference,
    DateOnly WorkDate,
    int Minutes,
    decimal HourlyRate,
    decimal Amount
);

public sealed record InputLeaveDeductionItem(
    int LeaveId,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate,
    int CalendarDays,
    decimal DeductionRate
);

public sealed record InputDeductionAgreementItem(
    long InstalmentId,
    long AgreementId,
    string Reference,
    string Category,
    decimal DueAmount
);

public sealed record InputAdjustmentItem(
    int AdjustmentId,
    string ComponentCode,
    string Description,
    decimal Amount,
    bool IsAddition
);
