namespace HRM.AuditLogs.Models;

public sealed record AuditLogResult(
    bool Succeeded,
    long? AuditLogId,
    Guid EventId,
    bool WasDuplicate = false,
    string? Error = null)
{
    public static AuditLogResult Success(long id, Guid eventId, bool duplicate = false)
        => new(true, id, eventId, duplicate, null);

    public static AuditLogResult Failure(Guid eventId, string error)
        => new(false, null, eventId, false, error);
}

public sealed class AuditLogDetailDto
{
    public long AuditLogDetailId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ValueType { get; set; } = "String";
    public bool IsRedacted { get; set; }
}

public sealed class AuditLogRecordDto
{
    public long AuditLogId { get; set; }
    public Guid EventId { get; set; }
    public int? TenantId { get; set; }
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public string? Url { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? MetadataJson { get; set; }
    public int SchemaVersion { get; set; }
    public DateTime? RetentionUntilUtc { get; set; }
    public bool IsLegalHold { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public int DetailCount { get; set; }
    public List<AuditLogDetailDto> Details { get; set; } = [];
}

public sealed class AuditLogPageResult
{
    public List<AuditLogRecordDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed class AuditExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv; charset=utf-8";
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public int RowCount { get; set; }
}
