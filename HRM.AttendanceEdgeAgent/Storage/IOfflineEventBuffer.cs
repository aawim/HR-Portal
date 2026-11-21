using HRM.AttendanceEdgeAgent.Ingestion;

namespace HRM.AttendanceEdgeAgent.Storage;

public sealed record BufferedBatch(
    string BatchId,
    DateTime CreatedAtUtc,
    int AttemptCount,
    DateTime? LastAttemptUtc,
    string? LastError,
    IReadOnlyList<AttendanceEnvelope> Events);

public sealed record BufferStatistics(
    int PendingBatches,
    int PendingEvents,
    int ArchivedBatches);

public interface IOfflineEventBuffer
{
    Task<int> EnqueueAsync(IReadOnlyList<AttendanceEnvelope> events, CancellationToken ct = default);
    Task<IReadOnlyList<BufferedBatch>> GetPendingBatchesAsync(int maxBatches = 10, CancellationToken ct = default);
    Task RecordAttemptAsync(string batchId, string? error = null, CancellationToken ct = default);
    Task AcknowledgeBatchAsync(string batchId, CancellationToken ct = default);
    Task<BufferStatistics> GetStatisticsAsync(CancellationToken ct = default);
}
