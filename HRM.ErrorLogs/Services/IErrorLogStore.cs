using HRM.ErrorLogs.Models;

namespace HRM.ErrorLogs.Services;

/// <summary>
/// Optional persistent storage backend for error logs (e.g. SQL Server table ERR_ErrorLogs).
/// </summary>
public interface IErrorLogStore
{
    Task PersistAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default);
    Task PersistBatchAsync(IReadOnlyList<ErrorLogEntry> entries, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ErrorLogEntry>> LoadRecentAsync(int count, bool includeDeleted = false, bool onlyDeleted = false, CancellationToken cancellationToken = default);
    Task<ErrorLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateResolutionAsync(Guid id, bool isResolved, string? resolvedBy, string? notes, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SoftDeleteResolvedAsync(string deletedBy, CancellationToken cancellationToken = default);
}
