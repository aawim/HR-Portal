namespace HRM.AuditLogs.Models;

public sealed class AuditLog
{
    public long AuditLogId { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
    public int? TenantId { get; set; }
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? UserId { get; set; }
    public string ActorType { get; set; } = "User";
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = "Committed";
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string Source { get; set; } = "HRM.Api";
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public DateTime? RetentionUntilUtc { get; set; }
    public bool IsLegalHold { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public ICollection<AuditLogDetail> Details { get; set; } = new List<AuditLogDetail>();
}
