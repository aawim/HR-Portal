using HRM.AuditLogs.Abstractions;

namespace HRM.AuditLogs.Models;

public sealed record AuditPropertyChange(
    string PropertyName,
    object? OldValue,
    object? NewValue,
    string? ValueType = null,
    bool ForceRedact = false);

public sealed class AuditLogRequest
{
    public Guid? EventId { get; set; }
    public int? TenantId { get; set; }
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? UserId { get; set; }
    public string? ActorType { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = AuditOutcomes.Committed;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? Source { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
    public object? Metadata { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsMandatory { get; set; } = true;
    public bool IsGlobalSystemEvent { get; set; }
    public bool IsLegalHold { get; set; }
    public List<AuditPropertyChange> Changes { get; set; } = [];
}
