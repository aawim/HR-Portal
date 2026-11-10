namespace HRM.ErrorLogs.Models;

/// <summary>
/// Aggregated statistics of application errors.
/// </summary>
public sealed class ErrorStatistics
{
    public int TotalErrors { get; set; }
    public int UnresolvedCount { get; set; }
    public int ResolvedCount { get; set; }
    public int FatalCount { get; set; }
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorsLast24Hours { get; set; }
    public Dictionary<string, int> SourceBreakdown { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<TopExceptionStat> TopExceptions { get; set; } = new();
}

public sealed class TopExceptionStat
{
    public string ExceptionType { get; set; } = string.Empty;
    public int Count { get; set; }
    public string SampleMessage { get; set; } = string.Empty;
}
