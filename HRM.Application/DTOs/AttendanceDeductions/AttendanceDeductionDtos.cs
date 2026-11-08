using System;
using System.Collections.Generic;

namespace HRM.DTOs.AttendanceDeductions;

public sealed record AttendanceDeductionPolicyDto(
    long PolicyId,
    int OrganisationId,
    string PolicyCode,
    string PolicyName,
    string LegalReference,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    bool IsVerified,
    int SalaryDivisor,
    string OfficialMinutesSource,
    int DefaultDailyWorkingMinutes,
    int ArrivalBufferMinutes,
    string BufferEligibilityCategory,
    bool BufferMakeUpRequired,
    string BufferMakeUpWindow,
    string BufferExceededRule,
    string LateDeductionFormula,
    string AbsenceDeductionFormula,
    bool DeductServiceAllowanceOnAbsence,
    string ServiceAllowanceComponentCode,
    bool DeductFromPensionableSalary,
    string RoundingMethod,
    bool RequiresVerification
);

public sealed record AttendanceAdjustmentDto(
    long AttendanceAdjustmentId,
    int OrganisationId,
    int IndividualId,
    string EmployeeName,
    string EmployeeNumber,
    string DepartmentName,
    int JobId,
    DateOnly WorkDate,
    string AdjustmentType,
    int ScheduledMinutes,
    int ActualMinutes,
    int DeductibleMinutes,
    decimal EstimatedDeductionAmount,
    decimal BasicSalaryDeductionAmount,
    decimal ServiceAllowanceDeductionAmount,
    string SourceAttendanceReference,
    string AttendanceOutcome,
    string VerificationStatus,
    int? VerifiedByUserId,
    string? VerifiedByUserName,
    DateTime? VerifiedAtUtc,
    string? ReviewerComments,
    string PolicyCode,
    string RegulatorySource,
    bool IsRegulatoryAssumptionVerified,
    string PayrollProcessingStatus,
    int? PayrollRunId,
    string? ScheduleDetails,
    string? ClockEventDetails
);

public sealed class VerifyAdjustmentInput
{
    public long AttendanceAdjustmentId { get; set; }
    public string Comments { get; set; } = string.Empty;
}

public sealed class ExcuseAdjustmentInput
{
    public long AttendanceAdjustmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class RejectAdjustmentInput
{
    public long AttendanceAdjustmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed record EvaluateDeductionRequestDto(
    int OrganisationId,
    int? IndividualId,
    int? JobId,
    DateOnly FromDate,
    DateOnly ToDate
);

public sealed record AttendanceDeductionCalculationPreviewDto(
    int DeductibleMinutes,
    decimal DailySalary,
    decimal PerMinuteSalary,
    decimal EstimatedLateDeduction,
    decimal EstimatedAbsenceDeduction,
    decimal TotalEstimatedDeduction,
    bool ReducesPensionBase,
    string PolicyCode,
    string LegalReference,
    string BufferOutcome
);
