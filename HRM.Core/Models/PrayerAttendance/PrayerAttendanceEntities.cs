namespace HRM.Models.PrayerAttendance;

public static class PrayerAttendanceArrangements
{
    public const string PrayerOnly = "PRAYER_ONLY";
    public const string Combined = "COMBINED";
    public const string SelectedPrayers = "SELECTED_PRAYERS";
    public const string RotatingPrayers = "ROTATING_PRAYERS";
    public const string TemporaryReplacement = "TEMPORARY_REPLACEMENT";
}

public static class PrayerSessionStatuses
{
    public const string Pending = "PENDING";
    public const string Present = "PRESENT";
    public const string Late = "LATE";
    public const string Absent = "ABSENT";
    public const string Incomplete = "INCOMPLETE";
    public const string Excused = "EXCUSED";
    public const string Replaced = "REPLACED";
    public const string PendingReview = "PENDING_REVIEW";
}

public sealed class AttendanceSchedulePolicy
{
    public long AttendanceSchedulePolicyId { get; set; }
    public int OrganisationId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Arrangement { get; set; } = PrayerAttendanceArrangements.PrayerOnly;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool AllowOverlappingSessions { get; set; }
    public bool AllowSharedEvidence { get; set; }
    public bool OvertimeEligible { get; set; }
    public bool SalaryDeductionEligible { get; set; }
    public string WorkingWeekPattern { get; set; } = "7_DAY";
    public string? ApprovalAuthority { get; set; }
    public string? SourceReference { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmployeeAttendancePolicyAssignment
{
    public long EmployeeAttendancePolicyAssignmentId { get; set; }
    public long AttendanceSchedulePolicyId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int? JobId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovalReference { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Mosque
{
    public long MosqueId { get; set; }
    public int OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IslandCode { get; set; } = string.Empty;
    public string? IslandName { get; set; }
    public string? LocationReference { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? AllowedRadiusMeters { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PrayerTimetableVersion
{
    public long PrayerTimetableVersionId { get; set; }
    public long MosqueId { get; set; }
    public int OrganisationId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = "DRAFT";
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? CorrectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PrayerTimetableEntry
{
    public long PrayerTimetableEntryId { get; set; }
    public long PrayerTimetableVersionId { get; set; }
    public DateOnly PrayerDate { get; set; }
    public string PrayerCode { get; set; } = string.Empty;
    public TimeOnly AdhanTime { get; set; }
    public TimeOnly? IqamahTime { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AttendanceRuleSet
{
    public long AttendanceRuleSetId { get; set; }
    public int OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int RequiredArrivalOffsetMinutes { get; set; }
    public int MinimumPhysicalPresenceMinutes { get; set; }
    public int CreditedWorkMinutes { get; set; }
    public int CheckInWindowBeforeMinutes { get; set; }
    public int CheckInWindowAfterMinutes { get; set; }
    public int CheckOutWindowBeforeMinutes { get; set; }
    public int CheckOutWindowAfterMinutes { get; set; }
    public string VerificationMethod { get; set; } = "BIOMETRIC";
    public string LateTreatment { get; set; } = "PENDING_REVIEW";
    public string EarlyDepartureTreatment { get; set; } = "PENDING_REVIEW";
    public string IncompleteSessionTreatment { get; set; } = "PENDING_REVIEW";
    public bool AllowEvidenceSharing { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SourceReference { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PrayerDutyAssignment
{
    public long PrayerDutyAssignmentId { get; set; }
    public long MosqueId { get; set; }
    public long AttendanceRuleSetId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int? JobId { get; set; }
    public string PrayerCode { get; set; } = string.Empty;
    public string RecurrencePattern { get; set; } = "WEEKLY";
    public int? DayOfWeek { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsExclusive { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? SourceReference { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PrayerDutyReplacement
{
    public long PrayerDutyReplacementId { get; set; }
    public long PrayerDutyAssignmentId { get; set; }
    public int OrganisationId { get; set; }
    public int OriginalIndividualId { get; set; }
    public int ReplacementIndividualId { get; set; }
    public int? ReplacementJobId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
    public string Status { get; set; } = "PENDING";
    public string Reason { get; set; } = string.Empty;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AttendanceSessionResult
{
    public long AttendanceSessionResultId { get; set; }
    public long WorkPlanId { get; set; }
    public int WorkPlanSegmentId { get; set; }
    public long? PrayerDutyAssignmentId { get; set; }
    public long? AttendanceRuleSetId { get; set; }
    public long? PrayerTimetableVersionId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public string SessionType { get; set; } = "PRAYER";
    public string Status { get; set; } = PrayerSessionStatuses.Pending;
    public int Version { get; set; } = 1;
    public int PhysicalPresenceMinutes { get; set; }
    public int CreditedWorkMinutes { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public string AlgorithmVersion { get; set; } = string.Empty;
    public string? EvaluationReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AttendanceEventMatch
{
    public long AttendanceEventMatchId { get; set; }
    public long AttendanceSessionResultId { get; set; }
    public int AttendanceLogId { get; set; }
    public string MatchRole { get; set; } = string.Empty;
    public string MatchMethod { get; set; } = string.Empty;
    public string ConfidenceState { get; set; } = "PENDING_REVIEW";
    public bool IsSharedEvidence { get; set; }
    public string? EvidenceSnapshot { get; set; }
    public DateTime MatchedAtUtc { get; set; } = DateTime.UtcNow;
}
