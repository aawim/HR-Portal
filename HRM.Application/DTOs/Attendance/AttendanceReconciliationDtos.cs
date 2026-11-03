using HRM.Enum;

namespace HRM.DTOs.Attendance;

public sealed class AttendanceEventAssociationDto
{
    public long? AttendanceLogResolutionId { get; set; }
    public int AttendanceLogId { get; set; }
    public long? WorkPlanId { get; set; }
    public long? WorkAssignmentId { get; set; }
    public int? WorkPlanSegmentId { get; set; }
    public string SchedulePurpose { get; set; } = "Official";
    public string ScheduleName { get; set; } = string.Empty;
    public string SegmentName { get; set; } = string.Empty;
    public AttendanceClockType ClockType { get; set; } = AttendanceClockType.Unresolved;
    public string LogicalEventType { get; set; } = "Unresolved";
    public bool IsPrimaryClassification { get; set; } = true;
    public string MatchMethod { get; set; } = "Deterministic";
    public string MatchReason { get; set; } = string.Empty;
    public string ConfidenceState { get; set; } = "Deterministic";
    public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyDepartureMinutes { get; set; }
}

public sealed class AttendanceResolvedAssociationDto
{
    public long? WorkPlanId { get; set; }
    public long? WorkAssignmentId { get; set; }
    public int? WorkPlanSegmentId { get; set; }
    public string SchedulePurpose { get; set; } = "Duty";
    public string SegmentName { get; set; } = string.Empty;
    public AttendanceClockType ClockType { get; set; } = AttendanceClockType.Unresolved;
    public string LogicalEventType { get; set; } = string.Empty;
    public bool IsPrimaryClassification { get; set; }
    public string MatchMethod { get; set; } = "PolicyShared";
    public string MatchReason { get; set; } = string.Empty;
    public string ConfidenceState { get; set; } = "PolicyShared";
    public DateTime? BoundaryTime { get; set; }
    public double DistanceMinutes { get; set; }
}

public sealed class AttendanceObligationSummaryDto
{
    public long? AttendanceObligationId { get; set; }
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
    public AttendanceObligationState State { get; set; } = AttendanceObligationState.NotStarted;
    public string StateLabel => State.ToString();
    public int? ResolvedStartAttendanceLogId { get; set; }
    public int? ResolvedEndAttendanceLogId { get; set; }
    public DateTime? ActualStartDateTime { get; set; }
    public DateTime? ActualEndDateTime { get; set; }
    public DateTime? BreakOutDateTime { get; set; }
    public DateTime? BreakInDateTime { get; set; }
    public int? BreakOutAttendanceLogId { get; set; }
    public int? BreakInAttendanceLogId { get; set; }
    public bool SharesStartEvent { get; set; }
    public bool SharesEndEvent { get; set; }
    public int PlannedMinutes { get; set; }
    public int ActualPresenceMinutes { get; set; }
    public int CreditedWorkedMinutes { get; set; }
    public int BreakDeductedMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyDepartureMinutes { get; set; }
    public int OvertimeCandidateMinutes { get; set; }
    public int OverlapWithOfficialMinutes { get; set; }
    public int OutsideOfficialMinutes { get; set; }
    public string StatusReason { get; set; } = string.Empty;
}

public sealed class AttendanceEventInterpretationDto
{
    public int AttendanceLogId { get; set; }
    public DateTime LogDateTime { get; set; }
    public int RecordedInOutModeId { get; set; }
    public int? ActualInOutModeId { get; set; }
    public int AttendanceLogModeId { get; set; }
    public bool IsDeviceDirectionExplicit { get; set; }
    public bool IsDuplicateSuppressed { get; set; }
    public bool IsSharedAcrossObligations => Associations.Count > 1;
    public string PrimaryPurpose { get; set; } = "Unresolved";
    public string PrimaryEventLabel { get; set; } = "Unresolved";
    public string ConfidenceState { get; set; } = "Deterministic";
    public string SummaryReason { get; set; } = string.Empty;
    public List<AttendanceEventAssociationDto> Associations { get; set; } = [];
}

public sealed class AttendanceReconciliationResult
{
    public long? ReconciliationRunId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";
    public AttendanceObligationState OfficialState { get; set; } = AttendanceObligationState.NotStarted;
    public AttendanceDailyStatus DailyStatus { get; set; } = AttendanceDailyStatus.Unknown;
    public DateTime? OfficialScheduledStart { get; set; }
    public DateTime? OfficialScheduledEnd { get; set; }
    public DateTime? OfficialCheckIn { get; set; }
    public DateTime? OfficialCheckOut { get; set; }
    public DateTime? OfficialBreakOut { get; set; }
    public DateTime? OfficialBreakIn { get; set; }
    public int? OfficialCheckInLogId { get; set; }
    public int? OfficialCheckOutLogId { get; set; }
    public int? OfficialBreakOutLogId { get; set; }
    public int? OfficialBreakInLogId { get; set; }
    public int PlannedOfficialMinutes { get; set; }
    public int ActualOfficialPresenceMinutes { get; set; }
    public int CreditedOfficialMinutes { get; set; }
    public int OfficialBreakMinutes { get; set; }
    public int ExcessBreakMinutes { get; set; }
    public int LateBreakReturnMinutes { get; set; }
    public int OfficialLateMinutes { get; set; }
    public int OfficialEarlyDepartureMinutes { get; set; }
    public int PlannedDutyMinutes { get; set; }
    public int ActualDutyPresenceMinutes { get; set; }
    public int CreditedDutyMinutes { get; set; }
    public int DutyWithinOfficialMinutes { get; set; }
    public int DutyOutsideOfficialMinutes { get; set; }
    public int TotalPhysicalPresenceMinutes { get; set; }
    public int TotalCreditedPayableMinutes { get; set; }
    public int OvertimeCandidateMinutes { get; set; }
    public int RawEventCount { get; set; }
    public int ResolvedEventCount { get; set; }
    public int UnresolvedEventCount { get; set; }
    public int SharedEventCount { get; set; }
    public int DuplicateSuppressedCount { get; set; }
    public int ConflictCount { get; set; }
    public bool HasConflicts => ConflictCount > 0;
    public bool IsComplete { get; set; }
    public string SummaryMessage { get; set; } = string.Empty;
    public List<AttendanceObligationSummaryDto> Obligations { get; set; } = [];
    public List<AttendanceEventInterpretationDto> Events { get; set; } = [];
    public List<AttendanceEvaluationIssueDto> Issues { get; set; } = [];
}

public sealed class AttendanceManualOverrideRequest
{
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public DateOnly WorkDate { get; set; }
    public int? AttendanceLogId { get; set; }
    public long? WorkPlanId { get; set; }
    public long? WorkAssignmentId { get; set; }
    public int? WorkPlanSegmentId { get; set; }
    public string OverrideAction { get; set; } = "AssignPrimary";
    public string TargetSchedulePurpose { get; set; } = "Official";
    public int? TargetClockTypeId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool AllowFinalizedPlanOverride { get; set; }
}

public sealed class AttendanceBatchReconciliationRequest
{
    public int OrganisationId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int? IndividualId { get; set; }
    public int? JobId { get; set; }
    public int? DepartmentStructureId { get; set; }
    public bool DryRunOnly { get; set; }
    public bool AllowFinalizedPlanOverride { get; set; }
    public string Reason { get; set; } = "Historical attendance schedule reconciliation";
}

public sealed class AttendanceBatchReconciliationDayDiffDto
{
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public string BeforeOfficialState { get; set; } = string.Empty;
    public string AfterOfficialState { get; set; } = string.Empty;
    public DateTime? BeforeOfficialCheckIn { get; set; }
    public DateTime? AfterOfficialCheckIn { get; set; }
    public DateTime? BeforeOfficialCheckOut { get; set; }
    public DateTime? AfterOfficialCheckOut { get; set; }
    public int BeforeWorkedMinutes { get; set; }
    public int AfterCreditedOfficialMinutes { get; set; }
    public int AfterCreditedDutyMinutes { get; set; }
    public int SharedEventsResolved { get; set; }
    public int ConflictsDetected { get; set; }
    public bool WasSkippedLockedOrFinalized { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
}

public sealed class AttendanceBatchReconciliationResult
{
    public long? ReconciliationRunId { get; set; }
    public bool IsDryRun { get; set; }
    public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";
    public int TotalEmployeeDaysEvaluated { get; set; }
    public int TotalDaysChanged { get; set; }
    public int TotalEventsReclassified { get; set; }
    public int TotalSharedEventsLinked { get; set; }
    public int TotalConflictsFlagged { get; set; }
    public int TotalSkippedFinalizedOrPayrollLocked { get; set; }
    public string SummaryMessage { get; set; } = string.Empty;
    public List<AttendanceBatchReconciliationDayDiffDto>Changes { get; set; } = [];
}
