namespace HRM.ActivityLogs.Abstractions;

public static class ActivityActorTypes
{
    public const string User = "User";
    public const string System = "System";
    public const string Integration = "Integration";
    public const string Delegated = "Delegated";
}

public sealed record ActivityActorContext
{
    public int? TenantId { get; init; }
    public int? OrganisationId { get; init; }
    public int? DivisionId { get; init; }
    public int? TeamId { get; init; }
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public string ActorType { get; init; } = ActivityActorTypes.User;
    public string? IpAddress { get; init; }
    public string? Url { get; init; }
    public string? CorrelationId { get; init; }
    public string? OpaqueSessionId { get; init; }
    public string Source { get; init; } = "HRM.Api";
    public bool IsSuperAdministrator { get; init; }
    public bool CanViewOrganisationActivity { get; init; }
    public bool CanViewTeamActivity { get; init; }
    public bool CanViewCrossOrganisationActivity { get; init; }
    public bool CanExportActivity { get; init; }
    public IReadOnlyCollection<int> AuthorizedOrganisationIds { get; init; } = Array.Empty<int>();
    public IReadOnlyCollection<int> AuthorizedDivisionIds { get; init; } = Array.Empty<int>();
    public IReadOnlyCollection<int> AuthorizedTeamIds { get; init; } = Array.Empty<int>();

    public static ActivityActorContext System(string source = "HRM.System", string? correlationId = null) => new()
    {
        ActorType = ActivityActorTypes.System,
        Source = source,
        CorrelationId = correlationId,
        IsSuperAdministrator = true,
        CanViewOrganisationActivity = true,
        CanViewTeamActivity = true,
        CanViewCrossOrganisationActivity = true,
        CanExportActivity = true
    };
}

public interface IActivityContextAccessor
{
    Task<ActivityActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default);
    void MarkActivityRecorded() { }
}

public interface IActivityClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemActivityClock : IActivityClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
