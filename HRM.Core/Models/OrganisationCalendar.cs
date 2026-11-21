namespace HRM.Models;

/// <summary>Organisation event (OC_ feature). Informational by default; only an approved
/// <see cref="OrganisationCalendarException"/> may change working-day behaviour.</summary>
public sealed class OrganisationEvent
{
    public int OrganisationEventId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string? EventCode { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Meeting";
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string TimeZoneId { get; set; } = "Indian/Maldives";
    public bool IsAllDay { get; set; }
    public string? VenueName { get; set; }
    public string? VenueAddress { get; set; }
    public string? MeetingUrl { get; set; }
    public int? OrganisingStructureId { get; set; }
    public int? ResponsibleIndividualId { get; set; }
    public string? ContactInformation { get; set; }
    public string? DressCode { get; set; }
    public string? AttendanceInstructions { get; set; }
    public string? RequiredItems { get; set; }
    public string? AdditionalNotes { get; set; }
    /// <summary>InformationOnly, Optional, Required, RequiredSelected.</summary>
    public string AttendanceRequirement { get; set; } = "InformationOnly";
    /// <summary>None, MarkAsNonWorkingDay, MarkAsWorkingDay, OverrideWorkingHours, ShortenedWorkingDay, ExtendedWorkingDay.</summary>
    public string CalendarImpactType { get; set; } = "None";
    public TimeSpan? EffectiveWorkStartTime { get; set; }
    public TimeSpan? EffectiveWorkEndTime { get; set; }
    public bool AffectsAttendanceExpectation { get; set; }
    public bool RequiresLeaveRecalculation { get; set; }
    /// <summary>Draft, PendingApproval, Published, Cancelled, Completed.</summary>
    public string Status { get; set; } = "Draft";
    public string Visibility { get; set; } = "Audience";
    public bool NotifyStaff { get; set; }
    public int? NotifyBeforeMinutes { get; set; }
    public DateTime? PublishDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public int? PublishedBy { get; set; }
    public bool IsValid { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public List<OrganisationEventAudience> Audiences { get; set; } = [];
    public OrganisationEventRecurrence? Recurrence { get; set; }
}

public sealed class OrganisationEventAudience
{
    public int OrganisationEventAudienceId { get; set; }
    public int OrganisationEventId { get; set; }
    /// <summary>Organisation, Department, Team, Individual, EmploymentCategory.</summary>
    public string AudienceType { get; set; } = "Organisation";
    public int? TargetEntityId { get; set; }
    public bool IsIncluded { get; set; } = true;
}

public sealed class OrganisationEventRecurrence
{
    public int OrganisationEventRecurrenceId { get; set; }
    public int OrganisationEventId { get; set; }
    /// <summary>Daily, Weekly, Monthly, Yearly.</summary>
    public string RecurrenceType { get; set; } = "Weekly";
    public int Interval { get; set; } = 1;
    /// <summary>Bitmask of DayOfWeek (1 &lt;&lt; (int)DayOfWeek), for weekly rules.</summary>
    public int WeekdayMask { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int? MaxOccurrences { get; set; }
    public List<OrganisationEventOccurrenceException> Exceptions { get; set; } = [];
}

/// <summary>Per-occurrence cancellation or modification of a recurring event.</summary>
public sealed class OrganisationEventOccurrenceException
{
    public int OrganisationEventOccurrenceExceptionId { get; set; }
    public int OrganisationEventRecurrenceId { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? OverrideStartDateTime { get; set; }
    public DateTime? OverrideEndDateTime { get; set; }
    public string? OverrideVenueName { get; set; }
    public string? Reason { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>Approved operational calendar override. Created only from a published event whose
/// CalendarImpactType is not None. Source of truth for IOrganisationCalendarResolver.</summary>
public sealed class OrganisationCalendarException
{
    public int OrganisationCalendarExceptionId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public int? OrganisationEventId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime EffectiveEndDate { get; set; }
    /// <summary>Organisation, Department, Team, Individual.</summary>
    public string TargetScope { get; set; } = "Organisation";
    public int? TargetEntityId { get; set; }
    public string CalendarImpactType { get; set; } = "MarkAsNonWorkingDay";
    public int Priority { get; set; }
    public string Reason { get; set; } = "";
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CreatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<OrganisationCalendarExceptionSegment> Segments { get; set; } = [];
}

/// <summary>One effective working period; several segments are allowed per exception.</summary>
public sealed class OrganisationCalendarExceptionSegment
{
    public int OrganisationCalendarExceptionSegmentId { get; set; }
    public int OrganisationCalendarExceptionId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

/// <summary>Result of evaluating an exception against a leave, leave plan or schedule.
/// Never mutates the source record; adjustments go through a review workflow.</summary>
public sealed class CalendarImpactAssessment
{
    public int CalendarImpactAssessmentId { get; set; }
    public int OrganisationCalendarExceptionId { get; set; }
    /// <summary>Leave, LeavePlan, WorkPlan, Schedule (validated by CK constraint).</summary>
    public string SourceRecordType { get; set; } = "Leave";
    public int SourceRecordId { get; set; }
    public int IndividualId { get; set; }
    public string AssessmentType { get; set; } = "LeaveDuration";
    public string? PreviousValue { get; set; }
    public string? ProposedValue { get; set; }
    public string? ImpactDetails { get; set; }
    /// <summary>Pending, Reviewed, Adjusted, Dismissed, Blocked.</summary>
    public string Status { get; set; } = "Pending";
    public bool IsLockedPeriod { get; set; }
    public DateTime AssessedDate { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>Deduplication log: one row per event/occurrence/recipient/reason; the message itself
/// is delivered through the existing SN_StaffNotifications table.</summary>
public sealed class OrganisationEventNotification
{
    public long OrganisationEventNotificationId { get; set; }
    public int OrganisationEventId { get; set; }
    public DateTime? OccurrenceDate { get; set; }
    public int IndividualId { get; set; }
    public string Reason { get; set; } = "Published";
    public long? StaffNotificationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Supervisor calendar task (UT_UserTodos is personal-only, has no team or time window).</summary>
public sealed class SupervisorCalendarTask
{
    public int SupervisorCalendarTaskId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public int? TeamId { get; set; }
    public int OwnerIndividualId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string TaskCategory { get; set; } = "General";
    public DateTime? StartDateTime { get; set; }
    public DateTime DueDateTime { get; set; }
    public string Priority { get; set; } = "Normal";
    /// <summary>NotStarted, InProgress, Completed, Cancelled.</summary>
    public string Status { get; set; } = "NotStarted";
    public int? ReminderMinutes { get; set; }
    public string? SourceModule { get; set; }
    public int? SourceRecordId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<SupervisorCalendarTaskAssignee> Assignees { get; set; } = [];
}

public sealed class SupervisorCalendarTaskAssignee
{
    public int SupervisorCalendarTaskId { get; set; }
    public int IndividualId { get; set; }
}

/// <summary>Append-only audit trail for events and calendar exceptions.</summary>
public sealed class OrganisationEventAudit
{
    public long OrganisationEventAuditId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public int OrganisationEventId { get; set; }
    public string Action { get; set; } = "";
    public int ActorIndividualId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
