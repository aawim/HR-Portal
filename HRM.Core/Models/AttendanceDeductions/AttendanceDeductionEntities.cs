using System;

namespace HRM.Models.AttendanceDeductions;

public static class AttendanceOutcomeClassifications
{
    public const string OnTime = "OnTime";
    public const string EligibleBuffer_Completed = "EligibleBuffer_Completed";
    public const string EligibleBuffer_MakeUpPending = "EligibleBuffer_MakeUpPending";
    public const string EligibleBuffer_MakeUpIncomplete = "EligibleBuffer_MakeUpIncomplete";
    public const string Late_Verified = "Late_Verified";
    public const string Late_Excused = "Late_Excused";
    public const string EarlyDeparture = "EarlyDeparture";
    public const string UnauthorisedAbsence = "UnauthorisedAbsence";
    public const string ApprovedPaidLeave = "ApprovedPaidLeave";
    public const string ApprovedUnpaidLeave = "ApprovedUnpaidLeave";
    public const string OfficialDuty = "OfficialDuty";
    public const string MissingClockEvent = "MissingClockEvent";
    public const string AttendanceConflict = "AttendanceConflict";
    public const string PendingVerification = "PendingVerification";
}

public static class AttendanceDeductionTypes
{
    public const string LateArrival = "LateArrival";
    public const string FullDayAbsence = "FullDayAbsence";
    public const string PartialDayAbsence = "PartialDayAbsence";
    public const string EarlyDeparture = "EarlyDeparture";
    public const string IncompleteMakeup = "IncompleteMakeup";
}

public static class AttendanceAdjustmentVerificationStatuses
{
    public const string PendingVerification = "PendingVerification";
    public const string Verified = "Verified";
    public const string Excused = "Excused";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}

public static class AttendanceAdjustmentPayrollStatuses
{
    public const string Pending = "Pending";
    public const string Ingested = "Ingested";
    public const string Calculated = "Calculated";
    public const string Locked = "Locked";
    public const string Paid = "Paid";
}

public static class BufferExceededRules
{
    public const string RequireReview = "RequireReview";
    public const string DeductAllMinutes = "DeductAllMinutes";
    public const string DeductExcessOnly = "DeductExcessOnly";
}

/// <summary>
/// Mapped to physical table ATD_AttendanceDeductionPolicies.
/// Stores effective-dated deduction policies according to Maldives Civil Service Regulations,
/// National Pay Commission framework, and organisation policies.
/// </summary>
public class AttendanceDeductionPolicy
{
    public long AttendanceDeductionPolicyId { get; set; }
    public int OrganisationId { get; set; }
    public string PolicyCode { get; set; } = "MCS-STD-2026";
    public string PolicyName { get; set; } = "Maldives Civil Service Standard Attendance Policy";
    public string LegalReference { get; set; } = "Civil Service Regulation Art 149 & 160; July 2026 Amendment";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = true;

    // Calculation parameters
    public int SalaryDivisor { get; set; } = 30; // Civil Service standard divisor
    public string OfficialMinutesSource { get; set; } = "ScheduledWorkPlan"; // "ScheduledWorkPlan", "Fixed360", "Fixed420"
    public int DefaultDailyWorkingMinutes { get; set; } = 360; // 6 hours = 360 minutes

    // July 2026 arrival buffer configuration
    public int ArrivalBufferMinutes { get; set; } = 30;
    public string BufferEligibilityCategory { get; set; } = "AllNonShift"; // "All", "AllNonShift", "OfficeOnly"
    public bool BufferMakeUpRequired { get; set; } = true;
    public string BufferMakeUpWindow { get; set; } = "SameWeek"; // "SameDay", "SameWeek", "SameMonth"
    public string BufferExceededRule { get; set; } = BufferExceededRules.RequireReview; // Prevents unverified hardcoding

    // Deduction formulas
    public string LateDeductionFormula { get; set; } = "PerMinuteBasic"; // (Basic/30/DailyMinutes) * LateMinutes
    public string AbsenceDeductionFormula { get; set; } = "BasicPlusServiceAllowance"; // Article 160(a)
    public bool DeductServiceAllowanceOnAbsence { get; set; } = true;
    public string ServiceAllowanceComponentCode { get; set; } = "SERV_ALLOW";
    public bool DeductFromPensionableSalary { get; set; } = true; // Maldives Pension Office Regulation 2021/R-27
    public string RoundingMethod { get; set; } = "AwayFromZero"; // "AwayFromZero", "ToEven", "Floor", "Ceiling"

    public bool RequiresVerification { get; set; } = true;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Mapped to physical table ATD_AttendanceAdjustments.
/// Bridge interface between Attendance Reconciliation and Enterprise Payroll.
/// Records verified attendance-related salary adjustments with strict idempotency.
/// </summary>
public class AttendanceAdjustment
{
    public long AttendanceAdjustmentId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string AdjustmentType { get; set; } = AttendanceDeductionTypes.LateArrival;

    // Attendance metrics
    public int ScheduledMinutes { get; set; }
    public int ActualMinutes { get; set; }
    public int DeductibleMinutes { get; set; }

    // Financial estimations (financial authority resides in Payroll)
    public decimal EstimatedDeductionAmount { get; set; }
    public decimal BasicSalaryDeductionAmount { get; set; }
    public decimal ServiceAllowanceDeductionAmount { get; set; }

    // Traceability & Idempotency
    public string SourceAttendanceReference { get; set; } = string.Empty;
    public long? SourceObligationId { get; set; }
    public string AttendanceOutcome { get; set; } = AttendanceOutcomeClassifications.PendingVerification;

    // Verification Workflow
    public string VerificationStatus { get; set; } = AttendanceAdjustmentVerificationStatuses.PendingVerification;
    public int? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? ReviewerComments { get; set; }

    // Policy Snapshot
    public long PolicyId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public string RegulatorySource { get; set; } = string.Empty;
    public bool IsRegulatoryAssumptionVerified { get; set; } = true;

    // Payroll Integration Status
    public string PayrollProcessingStatus { get; set; } = AttendanceAdjustmentPayrollStatuses.Pending;
    public int? PayrollRunId { get; set; }
    public int? PayrollEmployeeId { get; set; }
    public int? PayrollLineId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Mapped to physical table ATD_AttendanceExceptionReviews.
/// Immutable audit log for administrative review actions on attendance adjustments.
/// </summary>
public class AttendanceExceptionReview
{
    public long AttendanceExceptionReviewId { get; set; }
    public long AttendanceAdjustmentId { get; set; }
    public int OrganisationId { get; set; }
    public string Action { get; set; } = string.Empty; // "Verify", "Excuse", "ReturnForClarification", "Reject", "Reopen"
    public string Reason { get; set; } = string.Empty;
    public int PerformedByUserId { get; set; }
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}
