namespace HRM.Models.WorkPlanning;

/// <summary>Stable roster identity. Published content is held by an immutable version.</summary>
public sealed class WorkforceRoster
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public string Status { get; set; } = "Draft";
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

/// <summary>Auditable snapshot of roster content. A published version is never edited in place.</summary>
public sealed class WorkforceRosterVersion
{
    public int Id { get; set; }
    public int WorkforceRosterId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int? PublishedByUserId { get; set; }
    public string? ChangeReason { get; set; }
    public int OperationLogId { get; set; }
    public WorkforceRoster WorkforceRoster { get; set; } = null!;
}

/// <summary>An operational duty requirement, independent of the staff assigned to fulfil it.</summary>
public sealed class DutyOccurrence
{
    public long Id { get; set; }
    public int WorkforceRosterVersionId { get; set; }
    public int DutyTypeId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string TimeZoneId { get; set; } = "Indian/Maldives";
    public string? LocationName { get; set; }
    public int RequiredStaffCount { get; set; } = 1;
    public string Status { get; set; } = "Planned";
    public string? SourceKey { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
    public WorkforceRosterVersion WorkforceRosterVersion { get; set; } = null!;
    public DutyType DutyType { get; set; } = null!;
}

/// <summary>Staff responsibility for an occurrence; it does not replace the underlying requirement.</summary>
public sealed class DutyAssignment
{
    public long Id { get; set; }
    public long DutyOccurrenceId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public string Status { get; set; } = "Assigned";
    public DateTime AssignedAtUtc { get; set; }
    public int AssignedByUserId { get; set; }
    public string? AssignmentReason { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
    public DutyOccurrence DutyOccurrence { get; set; } = null!;
}
