namespace HRM.ActivityLogs.Models;

public sealed class ActivityLog
{
    public long ActivityLogId { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
    public int? TenantId { get; set; }
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? TeamId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string ActorType { get; set; } = "User";
    public string? IpAddress { get; set; }
    public string? Url { get; set; }
    public string ActivityCategory { get; set; } = Models.ActivityCategory.Administration.ToString();
    public string ActivityType { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string Source { get; set; } = "HRM.Api";
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ActivityOutboxMessage
{
    public long OutboxId { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string PayloadJson { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? LastError { get; set; }
}
