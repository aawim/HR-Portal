using HRM.ErrorLogs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRM.ErrorLogs.Services;

/// <summary>
/// Background worker that drains error logs from the bounded channel and flushes them in batches
/// to the persistent database store. Eliminates database I/O latency from the user request pipeline.
/// </summary>
public sealed class ErrorLogBackgroundProcessor : BackgroundService
{
    private readonly IErrorLogQueue _queue;
    private readonly IErrorLogStore? _store;
    private readonly ILogger<ErrorLogBackgroundProcessor>? _logger;
    private const int BatchSize = 50;

    public ErrorLogBackgroundProcessor(
        IErrorLogQueue queue,
        IErrorLogStore? store = null,
        ILogger<ErrorLogBackgroundProcessor>? logger = null)
    {
        _queue = queue;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_store == null)
        {
            _logger?.LogDebug("No persistent IErrorLogStore registered. ErrorLogBackgroundProcessor is idle.");
            return;
        }

        var batch = new List<ErrorLogEntry>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Await a single reader cleanly (never orphan a ReadAsync task on a SingleReader channel).
                var firstItem = await _queue.DequeueAsync(stoppingToken);
                batch.Add(firstItem);

                // Drain any additional immediately-ready entries up to BatchSize
                while (batch.Count < BatchSize && _queue.TryDequeue(out var nextItem))
                {
                    if (nextItem != null)
                        batch.Add(nextItem);
                }

                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch, stoppingToken);
                    batch.Clear();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Background error log batch processor encountered an unexpected error.");
                try { await Task.Delay(1000, stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }

        // Final graceful flush on application shutdown
        if (batch.Count > 0)
        {
            try
            {
                await FlushBatchAsync(batch, CancellationToken.None);
            }
            catch
            {
                // Suppress shutdown persistence exceptions
            }
        }
    }

    private async Task FlushBatchAsync(List<ErrorLogEntry> batch, CancellationToken cancellationToken)
    {
        if (_store == null || batch.Count == 0) return;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await _store.PersistBatchAsync(batch, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt == 1 && !cancellationToken.IsCancellationRequested)
            {
                _logger?.LogWarning(ex, "Transient failure persisting batch of {Count} error logs (attempt 1); retrying.", batch.Count);
                try { await Task.Delay(500, cancellationToken); } catch (OperationCanceledException) { return; }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist batch of {Count} error logs to storage.", batch.Count);
            }
        }
    }
}
