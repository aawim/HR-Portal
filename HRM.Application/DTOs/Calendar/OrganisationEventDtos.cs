namespace HRM.DTOs.Calendar;

public sealed class OrganisationEventAudienceDto
{
    /// <summary>Organisation, Department, Team, Individual, EmploymentCategory.</summary>
    public string AudienceType { get; set; } = "Organisation";
    public int? TargetEntityId { get; set; }
    public bool IsIncluded { get; set; } = true;
}

public sealed class OrganisationEventRecurrenceDto
{
    public string RecurrenceType { get; set; } = "Weekly";
    public int Interval { get; set; } = 1;
    public int WeekdayMask { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int? MaxOccurrences { get; set; }
}

public sealed class OrganisationEventDto
{
    public int OrganisationEventId { get; set; }
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
    public string AttendanceRequirement { get; set; } = "InformationOnly";
    public string CalendarImpactType { get; set; } = "None";
    public TimeSpan? EffectiveWorkStartTime { get; set; }
    public TimeSpan? EffectiveWorkEndTime { get; set; }
    public bool AffectsAttendanceExpectation { get; set; }
    public bool RequiresLeaveRecalculation { get; set; }
    public string Status { get; set; } = "Draft";
    public string Visibility { get; set; } = "Audience";
    public bool NotifyStaff { get; set; }
    public int? NotifyBeforeMinutes { get; set; }
    public DateTime? PublishDate { get; set; }
    public List<OrganisationEventAudienceDto> Audiences { get; set; } = [];
    public OrganisationEventRecurrenceDto? Recurrence { get; set; }
    public string AudienceSummary { get; set; } = "";
    public string? RowVersion { get; set; }
}

public sealed class OrganisationEventAuditDto
{
    public string Action { get; set; } = "";
    public int ActorIndividualId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class EventImpactPreviewDto
{
    public int OrganisationEventId { get; set; }
    public bool ChangesWorkSchedule { get; set; }
    /// <summary>Plain-language statement shown to the administrator before publishing.</summary>
    public string EffectSummary { get; set; } = "";
    public List<DateTime> AffectedDates { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Conflicts { get; set; } = [];
    public bool CanPublish { get; set; }
}
