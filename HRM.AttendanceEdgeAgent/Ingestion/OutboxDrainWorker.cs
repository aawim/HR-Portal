using HRM.AttendanceEdgeAgent.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRM.AttendanceEdgeAgent.Ingestion;

public sealed class OutboxDrainWorker : BackgroundService
{
    private readonly IOfflineEventBuffer _outbox;
    private readonly DhrIngestionClient _ingestion;
    private readonly ILogger<OutboxDrainWorker> _logger;

    public OutboxDrainWorker(
        IOfflineEventBuffer outbox,
        DhrIngestionClient ingestion,
        ILogger<OutboxDrainWorker> logger)
    {
        _outbox = outbox;
        _ingestion = ingestion;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox drain worker started. Monitoring local buffer for pending batches.");
        var consecutiveErrors = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var hadWork = false;
            try
            {
                var batches = await _outbox.GetPendingBatchesAsync(maxBatches: 5, stoppingToken);
                if (batches.Count > 0)
                {
                    hadWork = true;
                    foreach (var batch in batches)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        try
                        {
                            var result = await _ingestion.SendAsync(batch.Events, stoppingToken);
                            await _outbox.AcknowledgeBatchAsync(batch.BatchId, stoppingToken);
                            consecutiveErrors = 0;

                            _logger.LogInformation("Successfully delivered batch {BatchId} to DHR: {Accepted} accepted, {Dup} duplicates, {Rej} rejected.",
                                batch.BatchId, result?.Accepted ?? batch.Events.Count, result?.Duplicate ?? 0, result?.Rejected ?? 0);
                        }
                        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                        {
                            consecutiveErrors++;
                            await _outbox.RecordAttemptAsync(batch.BatchId, ex.Message, stoppingToken);
                            _logger.LogWarning("Failed to deliver batch {BatchId} to DHR (attempt #{Attempts}): {Message}",
                                batch.BatchId, batch.AttemptCount + 1, ex.Message);
                            break; // Stop draining further batches until connectivity is restored
                        }
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                consecutiveErrors++;
                _logger.LogError(ex, "Unexpected error reading or flushing outbox.");
            }

            // Adaptive backoff: fast drain when active (1s), normal poll when empty (5s), backoff when failing (15-60s)
            var delayMs = consecutiveErrors > 0
                ? Math.Min(60000, 10000 * consecutiveErrors)
                : (hadWork ? 1000 : 5000);

            await Task.Delay(delayMs, stoppingToken);
        }
    }
}
