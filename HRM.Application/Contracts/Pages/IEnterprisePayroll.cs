using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HRM.DTOs;

namespace HRM.Application.Contracts.Pages;

public sealed record PayrollPeriodInput(
    int Year,
    int Month,
    int? PayrollGroupId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    DateOnly? AttendanceCutoffDate = null,
    DateOnly? AdjustmentCutoffDate = null,
    DateOnly? PaymentDate = null
);

public sealed record PayrollPeriodRow(
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
    int PeriodStatus,
    int TotalRunsCount
);

public sealed record CreatePayrollRunInput(
    int PayrollPeriodId,
    int RunType,
    string Title,
    string? Description
);

public sealed record PayrollRunSummaryRow(
    int PayrollRunId,
    int OrganisationId,
    int PayrollPeriodId,
    int RunType,
    int Status,
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

public sealed record PayrollProgressRow(
    int PayrollRunId,
    int Status,
    int TotalEmployees,
    int ProcessedEmployees,
    int SuccessfulEmployees,
    int FailedEmployees,
    double PercentComplete,
    string CurrentStage,
    List<string> RecentErrors
);

public sealed record PayrollLineItemRow(
    int PayrollLineId,
    int PayrollEmployeeId,
    string ComponentCode,
    string ComponentName,
    int ComponentType,
    decimal Rate,
    decimal Units,
    decimal CalculatedAmount,
    bool IsTaxable,
    bool IsPensionable,
    string? SourceReference
);

public sealed record PayrollEmployeeRow(
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
    List<PayrollLineItemRow> Lines
);

public sealed record PayrollExceptionRow(
    int PayrollExceptionId,
    int PayrollRunId,
    int? JobId,
    int? IndividualId,
    int Severity,
    string Code,
    string Message,
    string? Details,
    bool IsAcknowledged
);

public sealed record PaymentBatchRow(
    int PaymentBatchId,
    int OrganisationId,
    int PayrollRunId,
    string BatchReference,
    string BankCode,
    string FormatCode,
    decimal TotalAmount,
    int TotalCount,
    int Status,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc
);

public sealed record PayslipRow(
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
    List<PayrollLineItemRow> Earnings,
    List<PayrollLineItemRow> Deductions,
    List<PayrollLineItemRow> EmployerItems
);

public sealed record PayrollConfigurationDto(
    decimal EmployeePensionPercentage,
    decimal EmployerPensionPercentage,
    decimal OvertimeMultiplier,
    int DefaultPaymentDay,
    int AttendanceCutoffDay,
    int AdjustmentCutoffDay
);

public sealed record UpdatePayrollConfigurationInput(
    decimal EmployeePensionPercentage,
    decimal EmployerPensionPercentage,
    decimal OvertimeMultiplier,
    int DefaultPaymentDay,
    int AttendanceCutoffDay,
    int AdjustmentCutoffDay
);

public sealed record UpdatePayrollPeriodInput(
    int PayrollPeriodId,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly AttendanceCutoffDate,
    DateOnly AdjustmentCutoffDate,
    DateOnly PaymentDate
);

public interface IEnterprisePayroll
{
    Task<PayrollConfigurationDto> GetConfigurationAsync();
    Task<ServiceResult> SaveConfigurationAsync(UpdatePayrollConfigurationInput input);
    Task<List<PayrollPeriodRow>> GetPeriodsAsync(int? year = null);
    Task<ServiceResult> CreatePeriodAsync(PayrollPeriodInput input);
    Task<ServiceResult> UpdatePeriodStatusAsync(int periodId, int newStatus);
    Task<ServiceResult> UpdatePeriodDatesAsync(UpdatePayrollPeriodInput input);
    Task<List<PayrollRunSummaryRow>> GetRunsAsync(int? periodId = null);
    Task<PayrollRunSummaryRow> GetRunDetailAsync(int runId);
    Task<PayrollProgressRow> GetProgressAsync(int runId);
    Task<ServiceResult> CreateRunAsync(CreatePayrollRunInput input);
    Task<ServiceResult> QueueCalculationAsync(int runId);
    Task<ServiceResult> SubmitReviewAsync(int runId, string? comment = null);
    Task<ServiceResult> ApprovePayrollAsync(int runId, string? comment = null);
    Task<ServiceResult> FinalisePayrollAsync(int runId, string? comment = null);
    Task<ServiceResult> RejectPayrollAsync(int runId, string reason);
    Task<List<PayrollEmployeeRow>> GetEmployeesAsync(int runId, int page = 1, int pageSize = 50, string? search = null);
    Task<List<PayrollExceptionRow>> GetExceptionsAsync(int runId);
    Task<List<PaymentBatchRow>> GetPaymentBatchesAsync(int? runId = null);
    Task<PaymentBatchRow> PreparePaymentBatchAsync(int runId, string bankCode, string formatCode);
    Task<string> ExportPaymentFileAsync(int paymentBatchId);
    Task<ServiceResult> ReconcilePaymentBatchAsync(int paymentBatchId, string reference);
    Task<List<PayslipRow>> GetMyPayslipsAsync();
    Task<PayslipRow> GetPayslipDetailAsync(int payrollEmployeeId);
}
