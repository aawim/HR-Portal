namespace HRM.ErrorLogs.Models;

/// <summary>
/// Comprehensive application error record capturing the exact location, cause, context, and fix recommendation.
/// Mapped to physical table ERR_ErrorLogs.
/// </summary>
public sealed class ErrorLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Timestamps
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public DateTime TimestampLocal { get; set; } = DateTime.Now;

    // Severity & Source
    public ErrorCriticalityLevel CriticalityLevel { get; set; } = ErrorCriticalityLevel.Error;
    public string ApplicationSource { get; set; } = "HRM.Api";
    public string Environment { get; set; } = "Development";

    // System Environment
    public string MachineName { get; set; } = System.Environment.MachineName;
    public int ProcessId { get; set; } = System.Environment.ProcessId;
    public int ThreadId { get; set; } = System.Threading.Thread.CurrentThread.ManagedThreadId;
    public string? TraceId { get; set; }

    // User & Request Context
    public string? Username { get; set; }
    public string? UserId { get; set; }
    public string? RemoteIp { get; set; }
    public string? PageLocation { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? UserAgent { get; set; }
    public string? QueryParameters { get; set; }

    // What Happened (Exception Details)
    public string ExceptionType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerExceptionType { get; set; }
    public string? InnerErrorMessage { get; set; }
    public string? InnerStackTrace { get; set; }

    // Error Location with exact line number
    public ErrorLocation Location { get; set; } = new();

    // Prescriptive Fix Guidance
    public FixRecommendation Fix { get; set; } = new();

    // Additional structured context (JSON)
    public string? CustomDataJson { get; set; }

    // Resolution Tracking
    public bool IsResolved { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}
