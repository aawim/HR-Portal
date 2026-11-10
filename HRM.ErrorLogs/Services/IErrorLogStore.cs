using HRM.ErrorLogs.Models;

namespace HRM.ErrorLogs.Services;

/// <summary>
/// Optional persistent storage backend for error logs (e.g. SQL Server table ERR_ErrorLogs).
/// </summary>
public interface IErrorLogStore
{
    Task PersistAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default);
    Task PersistBatchAsync(IReadOnlyList<ErrorLogEntry> entries, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ErrorLogEntry>> LoadRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<ErrorLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateResolutionAsync(Guid id, bool isResolved, string? resolvedBy, string? notes, CancellationToken cancellationToken = default);
    Task<int> ClearResolvedAsync(CancellationToken cancellationToken = default);
}
