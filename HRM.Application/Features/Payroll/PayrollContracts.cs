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
    List<InputAdjustmentItem> Adjustments,
    List<InputAttendanceDeductionItem>? AttendanceDeductions = null,
    bool IsMaldivianCitizen = true,
    DateOnly? DateOfBirth = null,
    PayrollStatutoryPolicy? FrozenPolicy = null,
    PayrollTaxContext? TaxContext = null,
    bool NationalityVerified = true,
    IReadOnlyList<string>? ValidationErrors = null,
    string BankIdentityNumber = ""
);

public sealed record PayrollTaxMonth(int Month, decimal TaxableRemuneration, decimal TaxWithheld);
public sealed record PayrollTaxContext(
    HRM.Application.Contracts.Pages.PayrollEmployeeTaxProfile? Profile = null,
    IReadOnlyList<PayrollTaxMonth>? PreviousMonths = null,
    bool HistoryComplete = false);

public sealed record InputAttendanceDeductionItem(
    long AttendanceAdjustmentId,
    string SourceReference,
    DateOnly WorkDate,
    string DeductionType,
    int ScheduledMinutes,
    int ActualMinutes,
    int DeductibleMinutes,
    decimal DeductionAmount,
    bool AffectsBasicSalary,
    bool AffectsServiceAllowance,
    string PolicyCode,
    string LegalReference
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
    bool IsAddition,
    bool IsTaxable = true
);

public sealed record PayrollStatutoryPolicy(
    decimal EmployeePensionRate = 0.07m,
    decimal EmployerPensionRate = 0.07m,
    bool AllowancesArePensionable = false,
    bool PensionMaldiviansOnly = true,
    int PensionMinAge = 16,
    int PensionMaxAge = 65,
    decimal NormalOvertimeMultiplier = 1.25m,
    decimal HolidayOvertimeMultiplier = 1.50m,
    decimal OvertimeHourlyDivisor = 180m,
    decimal MinimumWageMonthly = 7000m,
    decimal MaxDeductionPercentage = 40m,
    decimal RamadanAllowanceAmount = 3000m,
    string UnpaidLeaveDivisorMode = "CalendarDays",
    bool RequireBankAccountForPayment = true,
    string DefaultCurrencyCode = "MVR",
    IReadOnlyList<HRM.Application.Contracts.Pages.PayrollTaxBracketDto>? TaxBrackets = null,
    string EmploymentSector = "CivilService",
    bool MinimumWageMaldiviansOnly = true,
    IReadOnlyList<HRM.Application.Contracts.Pages.PayrollEmployeeTaxProfile>? EmployeeTaxProfiles = null
)
{
    public static IReadOnlyList<HRM.Application.Contracts.Pages.PayrollTaxBracketDto> MaldivesDefaultTaxBrackets { get; } =
    [
        new(0m, 60000m, 0m),
        new(60000m, 100000m, 5.5m),
        new(100000m, 150000m, 8.0m),
        new(150000m, 200000m, 12.0m),
        new(200000m, null, 15.0m)
    ];

    public static PayrollStatutoryPolicy MaldivesDefault { get; } = new(TaxBrackets: MaldivesDefaultTaxBrackets);
}
