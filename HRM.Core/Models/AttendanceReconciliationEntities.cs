using System;
using HRM.Models.WorkPlanning;

namespace HRM.Models;

public class AttendanceReconciliationRun
{
    public long AttendanceReconciliationRunId { get; set; }
    public int OrganisationId { get; set; }
    public int? IndividualId { get; set; }
    public int? JobId { get; set; }
    public DateTime FromWorkDate { get; set; }
    public DateTime ToWorkDate { get; set; }
    public string TriggerSource { get; set; } = "DailyEvaluation";
    public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";
    public int? InitiatedByUserId { get; set; }
    public int? OperationLogId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int TotalDaysEvaluated { get; set; }
    public int TotalEventsProcessed { get; set; }
    public int ConflictCount { get; set; }
    public string Status { get; set; } = "Completed";
    public string? SummaryMessage { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class AttendanceObligation
{
    public long AttendanceObligationId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public DateTime WorkDate { get; set; }
    public long? WorkPlanId { get; set; }
    public long? WorkAssignmentId { get; set; }
    public int? WorkPlanSegmentId { get; set; }
    public string ObligationType { get; set; } = "Official";
    public string ScheduleName { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public int GraceBeforeMinutes { get; set; }
    public int GraceAfterMinutes { get; set; }
    public bool RequiresAttendance { get; set; } = true;
    public bool RequiresCheckOut { get; set; } = true;
    public string ObligationState { get; set; } = "NotStarted";
    public int? ResolvedStartAttendanceLogId { get; set; }
    public int? ResolvedEndAttendanceLogId { get; set; }
    public DateTime? ActualStartDateTime { get; set; }
    public DateTime? ActualEndDateTime { get; set; }
    public int PlannedMinutes { get; set; }
    public int ActualPresenceMinutes { get; set; }
    public int CreditedWorkedMinutes { get; set; }
    public int BreakDeductedMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyDepartureMinutes { get; set; }
    public int OvertimeCandidateMinutes { get; set; }
    public int OverlapWithOfficialMinutes { get; set; }
    public int OutsideOfficialMinutes { get; set; }
    public string? StatusReason { get; set; }
    public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";
    public long? ReconciliationRunId { get; set; }
    public bool IsValid { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; }
}

public class AttendanceManualOverride
{
    public long AttendanceManualOverrideId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public DateTime WorkDate { get; set; }
    public int? AttendanceLogId { get; set; }
    public long? WorkPlanId { get; set; }
    public long? WorkAssignmentId { get; set; }
    public int? WorkPlanSegmentId { get; set; }
    public string OverrideAction { get; set; } = "AssignPrimary";
    public string TargetSchedulePurpose { get; set; } = "Official";
    public int? TargetClockTypeId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long? SupersededByOverrideId { get; set; }
    public int? OperationLogId { get; set; }
    public bool IsActive { get; set; } = true;
}
