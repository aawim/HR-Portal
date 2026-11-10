using HRM.ErrorLogs.Models;

namespace HRM.ErrorLogs.Services;

/// <summary>
/// Central contract for error recording, diagnostic analysis, and log management.
/// </summary>
public interface IErrorLogService
{
    /// <summary>
    /// Captures an exception, resolves exact source line numbers, computes prescriptive fix guidance, and records the error.
    /// </summary>
    Task<ErrorLogEntry> CaptureExceptionAsync(Exception ex, ErrorCriticalityLevel level = ErrorCriticalityLevel.Error, ErrorContext? context = null);

    /// <summary>
    /// Records a pre-constructed ErrorLogEntry.
    /// </summary>
    Task<ErrorLogEntry> LogAsync(ErrorLogEntry entry);

    /// <summary>
    /// Retrieves recent errors matching optional filters.
    /// </summary>
    Task<IReadOnlyList<ErrorLogEntry>> GetRecentErrorsAsync(
        int count = 100,
        ErrorCriticalityLevel? minLevel = null,
        string? search = null,
        bool includeDeleted = false,
        bool onlyDeleted = false);

    /// <summary>
    /// Retrieves full details of a specific error by its unique identifier.
    /// </summary>
    Task<ErrorLogEntry?> GetErrorByIdAsync(Guid id);

    /// <summary>
    /// Marks an error as resolved by a developer with optional notes.
    /// </summary>
    Task<bool> MarkResolvedAsync(Guid id, string resolvedBy, string? notes = null);

    /// <summary>
    /// Reopens a previously resolved error.
    /// </summary>
    Task<bool> ReopenAsync(Guid id);

    /// <summary>
    /// Soft-deletes an error record (never physically deletes from database).
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid id, string deletedBy);

    /// <summary>
    /// Restores a soft-deleted error record back to active.
    /// </summary>
    Task<bool> RestoreAsync(Guid id);

    /// <summary>
    /// Soft-deletes all resolved error entries (preserves database records permanently).
    /// </summary>
    Task<int> SoftDeleteResolvedAsync(string deletedBy = "Developer");

    /// <summary>
    /// Clears resolved error entries via soft-deletion.
    /// </summary>
    Task<int> ClearResolvedAsync(string deletedBy = "Developer");

    /// <summary>
    /// Computes summary statistics across all logged errors.
    /// </summary>
    Task<ErrorStatistics> GetStatisticsAsync();
}
