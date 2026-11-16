using System.Text.Json;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AttendanceEdgeAgent.Storage;

public sealed class FileOfflineEventBuffer : IOfflineEventBuffer
{
    private readonly string _outboxDir;
    private readonly string _archiveDir;
    private readonly int _maxBatchSize;
    private readonly ILogger<FileOfflineEventBuffer> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public FileOfflineEventBuffer(IOptions<AttendanceEdgeOptions> options, ILogger<FileOfflineEventBuffer> logger)
    {
        _logger = logger;
        _maxBatchSize = Math.Clamp(options.Value.MaxBatchSize, 1, 500);
        _outboxDir = Path.Combine(options.Value.DataDirectory, "outbox");
        _archiveDir = Path.Combine(options.Value.DataDirectory, "archive");
        Directory.CreateDirectory(_outboxDir);
        Directory.CreateDirectory(_archiveDir);
    }

    public async Task<int> EnqueueAsync(IReadOnlyList<AttendanceEnvelope> events, CancellationToken ct = default)
    {
        if (events == null || events.Count == 0) return 0;

        await _gate.WaitAsync(ct);
        try
        {
            var enqueuedCount = 0;
            var chunks = events.Chunk(_maxBatchSize);
            var now = DateTime.UtcNow;

            foreach (var chunk in chunks)
            {
                var batchId = $"{now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
                var batch = new BufferedBatch(
                    BatchId: batchId,
                    CreatedAtUtc: now,
                    AttemptCount: 0,
                    LastAttemptUtc: null,
                    LastError: null,
                    Events: chunk.ToList());

                var filePath = Path.Combine(_outboxDir, $"{batchId}.json");
                var tempPath = Path.Combine(_outboxDir, $"{batchId}.tmp.{Guid.NewGuid():N}");

                await using (var stream = File.Create(tempPath))
                {
                    await JsonSerializer.SerializeAsync(stream, batch, JsonOptions, ct);
                }
                File.Move(tempPath, filePath, overwrite: true);
                enqueuedCount += chunk.Length;
            }

            _logger.LogInformation("Enqueued {Count} attendance events across outbox batches.", enqueuedCount);
            return enqueuedCount;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<BufferedBatch>> GetPendingBatchesAsync(int maxBatches = 10, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var files = Directory.GetFiles(_outboxDir, "*.json")
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
                .Take(maxBatches)
                .ToList();

            var list = new List<BufferedBatch>();
            foreach (var file in files)
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    var batch = await JsonSerializer.DeserializeAsync<BufferedBatch>(stream, JsonOptions, ct);
                    if (batch != null) list.Add(batch);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read outbox batch file {File}.", file);
                }
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordAttemptAsync(string batchId, string? error = null, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var filePath = Path.Combine(_outboxDir, $"{batchId}.json");
            if (!File.Exists(filePath)) return;

            BufferedBatch? batch;
            await using (var readStream = File.OpenRead(filePath))
            {
                batch = await JsonSerializer.DeserializeAsync<BufferedBatch>(readStream, JsonOptions, ct);
            }

            if (batch == null) return;

            var updated = batch with
            {
                AttemptCount = batch.AttemptCount + 1,
                LastAttemptUtc = DateTime.UtcNow,
                LastError = error
            };

            var tempPath = Path.Combine(_outboxDir, $"{batchId}.tmp.{Guid.NewGuid():N}");
            await using (var writeStream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(writeStream, updated, JsonOptions, ct);
            }
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record attempt for batch {BatchId}.", batchId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AcknowledgeBatchAsync(string batchId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var filePath = Path.Combine(_outboxDir, $"{batchId}.json");
            if (!File.Exists(filePath)) return;

            var archivePath = Path.Combine(_archiveDir, $"{batchId}.json");
            File.Move(filePath, archivePath, overwrite: true);

            // Clean up archive files older than 7 days
            PruneArchive();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge outbox batch {BatchId}.", batchId);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<BufferStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var outboxFiles = Directory.GetFiles(_outboxDir, "*.json");
            var archiveFiles = Directory.GetFiles(_archiveDir, "*.json");
            var pendingEvents = 0;

            foreach (var file in outboxFiles)
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    var batch = await JsonSerializer.DeserializeAsync<BufferedBatch>(stream, JsonOptions, ct);
                    if (batch != null) pendingEvents += batch.Events.Count;
                }
                catch
                {
                    // Ignore corrupted file during stats check
                }
            }

            return new BufferStatistics(
                PendingBatches: outboxFiles.Length,
                PendingEvents: pendingEvents,
                ArchivedBatches: archiveFiles.Length);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void PruneArchive()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            foreach (var file in Directory.GetFiles(_archiveDir, "*.json"))
            {
                if (File.GetCreationTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Archive pruning encountered an error.");
        }
    }
}
