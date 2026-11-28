namespace HRM.ErrorLogs.Models;

/// <summary>
/// Filter criteria for querying error logs.
/// </summary>
public sealed class ErrorLogFilter
{
    public string? SearchText { get; set; }
    public string? ApplicationSource { get; set; }
    public ErrorCriticalityLevel? MinLevel { get; set; }
    public bool? IsResolved { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
