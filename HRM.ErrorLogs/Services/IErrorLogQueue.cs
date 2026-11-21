using HRM.ErrorLogs.Models;

namespace HRM.ErrorLogs.Services;

/// <summary>
/// High-throughput, non-blocking in-memory queue for error log entries.
/// </summary>
public interface IErrorLogQueue
{
    /// <summary>
    /// Enqueues an error entry into the bounded buffer without blocking the caller.
    /// Returns true if accepted; false if rejected due to full capacity.
    /// </summary>
    bool Enqueue(ErrorLogEntry entry);

    /// <summary>
    /// Reads an entry asynchronously when available.
    /// </summary>
    ValueTask<ErrorLogEntry> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams available items asynchronously.
    /// </summary>
    IAsyncEnumerable<ErrorLogEntry> ReadAllAsync(CancellationToken cancellationToken = default);
}
