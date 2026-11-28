namespace HRM.AuditLogs.Models;

public sealed class AuditLogFilter
{
    public int? OrganisationId { get; set; }
    public int? DivisionId { get; set; }
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? Outcome { get; set; }
    public string? Module { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public string? SearchText { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public bool IncludeDetails { get; set; }
    public string SortBy { get; set; } = "OccurredAtUtc";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
