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
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(500);

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
                var timeoutTask = Task.Delay(FlushInterval, stoppingToken);
                var dequeueTask = _queue.DequeueAsync(stoppingToken).AsTask();

                var completedTask = await Task.WhenAny(dequeueTask, timeoutTask);

                if (completedTask == dequeueTask)
                {
                    var firstItem = await dequeueTask;
                    batch.Add(firstItem);

                    // Drain whatever else is currently ready in the channel up to BatchSize
                    while (batch.Count < BatchSize)
                    {
                        using var drainCts = new CancellationTokenSource(10);
                        try
                        {
                            var nextItem = await _queue.DequeueAsync(drainCts.Token);
                            batch.Add(nextItem);
                        }
                        catch
                        {
                            break; // Channel is empty for now
                        }
                    }
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
                await Task.Delay(1000, stoppingToken);
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

        try
        {
            await _store.PersistBatchAsync(batch, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist batch of {Count} error logs to storage.", batch.Count);
        }
    }
}
