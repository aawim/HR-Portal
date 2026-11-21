namespace HRM.ErrorLogs.Models;

/// <summary>
/// Severity / criticality level of an error log entry.
/// </summary>
public enum ErrorCriticalityLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3,
    Fatal = 4
}
