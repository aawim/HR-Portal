namespace HRM.ActivityLogs.Models;

public sealed class ActivityLogRequest
{
    public Guid? EventId { get; set; }
    public int? TenantId { get; set; }
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? TeamId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ActorType { get; set; }
    public string? IpAddress { get; set; }
    public string? Url { get; set; }
    public ActivityCategory Category { get; set; } = ActivityCategory.Administration;
    public string ActivityType { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? CorrelationId { get; set; }
    public string? Source { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
    public bool RequireGuaranteedDelivery { get; set; }
}

public sealed class ActivityLogFilter
{
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? TeamId { get; set; }
    public int? UserId { get; set; }
    public string? Category { get; set; }
    public string? ActivityType { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? CorrelationId { get; set; }
    public string? SearchText { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string SortBy { get; set; } = "OccurredAtUtc";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class ActivityLogDto
{
    public long ActivityLogId { get; set; }
    public Guid EventId { get; set; }
    public int? TenantId { get; set; }
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? TeamId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Url { get; set; }
    public string ActivityCategory { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetEntityType { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ActivityLogPageResult
{
    public List<ActivityLogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed class ActivityExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv; charset=utf-8";
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public int RowCount { get; set; }
}
