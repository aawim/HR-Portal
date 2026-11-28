namespace HRM.ActivityLogs.Models;

public sealed class ActivityLogOptions
{
    public const string SectionName = "ActivityLogs";

    public string ConnectionStringName { get; set; } = "ActivityLogsConnection";
    public string? ConnectionString { get; set; }
    public string Schema { get; set; } = "activity";
    public string DefaultSource { get; set; } = "HRM.Api";

    /// <summary>
    /// Enables bounded asynchronous queue processing for noncritical activity events.
    /// </summary>
    public bool EnableAsyncQueue { get; set; } = true;

    /// <summary>
    /// Bounded queue capacity to enforce backpressure and prevent unbounded memory growth.
    /// </summary>
    public int QueueCapacity { get; set; } = 4096;

    /// <summary>
    /// Maximum retry attempts for failed outbox deliveries.
    /// </summary>
    public int MaxOutboxRetries { get; set; } = 5;

    /// <summary>
    /// Polling interval in seconds for outbox message dispatch.
    /// </summary>
    public int OutboxPollSeconds { get; set; } = 5;

    /// <summary>
    /// Default retention period in days before old activity logs are eligible for cleanup.
    /// </summary>
    public int DefaultRetentionDays { get; set; } = 365;

    /// <summary>
    /// Cleanup interval in hours for ActivityRetentionProcessor.
    /// </summary>
    public int RetentionCheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Maximum batch size deleted per retention cycle.
    /// </summary>
    public int RetentionBatchSize { get; set; } = 1000;

    /// <summary>
    /// Maximum page size allowed for query endpoints.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Maximum rows allowed in a single CSV export.
    /// </summary>
    public int MaxExportRows { get; set; } = 5000;

    /// <summary>
    /// Maximum length for MetadataJson.
    /// </summary>
    public int MaxMetadataLength { get; set; } = 4000;

    /// <summary>
    /// Whether to capture IP addresses when available on the actor context.
    /// </summary>
    public bool CaptureIpAddress { get; set; } = true;

    /// <summary>
    /// Categories disabled here will not be recorded.
    /// </summary>
    public HashSet<string> DisabledCategories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional per-category retention overrides in days.
    /// </summary>
    public Dictionary<string, int> CategoryRetentionDays { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
