namespace HRM.AuditLogs.Models;

public sealed class AuditLogDetail
{
    public long AuditLogDetailId { get; set; }
    public long AuditLogId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ValueType { get; set; } = "String";
    public bool IsRedacted { get; set; }

    public AuditLog AuditLog { get; set; } = null!;
}
