namespace HRM.AuditLogs.Abstractions;

public static class AuditActorTypes
{
    public const string User = "User";
    public const string System = "System";
    public const string Integration = "Integration";
    public const string Delegated = "Delegated";
}

public static class AuditOutcomes
{
    public const string Committed = "Committed";
    public const string Denied = "Denied";
    public const string Failed = "Failed";
}

public sealed record AuditActorContext
{
    public int? TenantId { get; init; }
    public int? OrganisationId { get; init; }
    public int? DivisionId { get; init; }
    public int? UserId { get; init; }
    public string ActorType { get; init; } = AuditActorTypes.User;
    public string? Username { get; init; }
    public string? CorrelationId { get; init; }
    public string? RequestId { get; init; }
    public string Source { get; init; } = "HRM.Api";
    public string? IpAddress { get; init; }
    public string? Url { get; init; }
    public bool IsSuperAdministrator { get; init; }
    public bool CanViewAuditLogs { get; init; }
    public bool CanViewConfidentialValues { get; init; }
    public bool CanExportAuditLogs { get; init; }
    public bool CanViewCrossOrganisationAudit { get; init; }
    public IReadOnlyCollection<int> AuthorizedOrganisationIds { get; init; } = Array.Empty<int>();
    public IReadOnlyCollection<int> AuthorizedDivisionIds { get; init; } = Array.Empty<int>();

    public static AuditActorContext System(string source = "HRM.System", string? correlationId = null) => new()
    {
        ActorType = AuditActorTypes.System,
        Source = source,
        CorrelationId = correlationId,
        IsSuperAdministrator = true,
        CanViewAuditLogs = true,
        CanViewConfidentialValues = true,
        CanExportAuditLogs = true,
        CanViewCrossOrganisationAudit = true
    };
}

public interface IAuditContextAccessor
{
    AuditActorContext? GetCurrentContext();
    Task<AuditActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default);
}

public interface IAuditClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemAuditClock : IAuditClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
