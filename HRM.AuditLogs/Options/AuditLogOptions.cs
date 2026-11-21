using HRM.AuditLogs.Models;

namespace HRM.AuditLogs.Options;

public sealed class AuditLogOptions
{
    public const string SectionName = "AuditLogs";

    /// <summary>
    /// Connection string name used by AuditDbContext. Defaults to "DefaultConnection"
    /// so audit.AuditLogs can participate in atomic SQL Server transactions with business tables.
    /// </summary>
    public string ConnectionStringName { get; set; } = "DefaultConnection";

    /// <summary>
    /// Explicit connection string override if provided directly.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Dedicated SQL schema for audit tables.
    /// </summary>
    public string Schema { get; set; } = "audit";

    /// <summary>
    /// Enables automatic EF Core SaveChanges change-tracking auditing.
    /// </summary>
    public bool EnableAutomaticAuditing { get; set; } = true;

    /// <summary>
    /// When true, failure to persist mandatory audit records aborts the business transaction.
    /// </summary>
    public bool FailOnMandatoryAuditError { get; set; } = true;

    /// <summary>
    /// Default source label when actor context does not provide one.
    /// </summary>
    public string DefaultSource { get; set; } = "HRM.Api";

    /// <summary>
    /// Default retention period in days (minimum 7 years = 2555 days for public-sector HR records).
    /// Retention metadata is recorded on each entry; records are never auto-deleted solely due to expiry.
    /// </summary>
    public int DefaultRetentionDays { get; set; } = 2555;

    /// <summary>
    /// Maximum serialized character length per OldValue / NewValue detail field.
    /// </summary>
    public int MaxValueLength { get; set; } = 4000;

    /// <summary>
    /// Maximum serialized character length for MetadataJson.
    /// </summary>
    public int MaxMetadataLength { get; set; } = 8000;

    /// <summary>
    /// Maximum page size allowed for audit search queries.
    /// </summary>
    public int MaxPageSize { get; set; } = 200;

    /// <summary>
    /// Maximum rows allowed in a single CSV export.
    /// </summary>
    public int MaxExportRows { get; set; } = 5000;

    /// <summary>
    /// Whether to capture IP addresses when available on the actor context.
    /// </summary>
    public bool CaptureIpAddress { get; set; } = true;

    /// <summary>
    /// Audit policy defining audited entities, excluded fields, redaction patterns, and confidential fields.
    /// </summary>
    public AuditLogPolicy Policy { get; set; } = AuditLogPolicy.CreateDefault();
}
