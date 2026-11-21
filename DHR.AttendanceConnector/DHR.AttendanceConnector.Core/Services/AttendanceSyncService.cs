using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using Microsoft.Extensions.Logging;

namespace DHR.AttendanceConnector.Core.Services;

public sealed record SyncExecutionSummary(int BatchesSent, int Accepted, int Duplicate, int Rejected, bool Succeeded, string? ErrorMessage = null);

public sealed class AttendanceSyncService(
    IConnectorRepository repository,
    IDhrApiClient apiClient,
    ILogger<AttendanceSyncService> logger)
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public async Task<SyncExecutionSummary> DrainPendingAsync(CancellationToken ct = default)
    {
        if (!await _syncLock.WaitAsync(0, ct))
            return new SyncExecutionSummary(0, 0, 0, 0, true, "Sync already in progress.");

        try
        {
            var profile = await repository.GetProfileAsync(ct);
            if (profile is null)
                return new SyncExecutionSummary(0, 0, 0, 0, false, "Connector is not activated.");

            // Recover any events left in Uploading state after an unexpected crash/power loss
            await repository.RecoverStaleUploadingEventsAsync(TimeSpan.FromMinutes(2), ct);

            var batchesSent = 0;
            var totalAccepted = 0;
            var totalDuplicate = 0;
            var totalRejected = 0;
            var batchSize = Math.Clamp(profile.SyncBatchSize, 1, 500);

            while (!ct.IsCancellationRequested)
            {
                var batchId = $"batch-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
                var claimed = await repository.ClaimPendingBatchAsync(batchSize, batchId, ct);
                if (claimed.Count == 0)
                    break;

                var localIds = claimed.Select(e => e.LocalId).ToList();
                var payload = claimed.Select(e => new RemoteBatchEventItem(
                    DeviceId: e.DeviceId,
                    LocalUserId: e.DeviceUserId,
                    DeviceTimestamp: DateTime.SpecifyKind(e.DeviceTimestampUtc, DateTimeKind.Utc),
                    DeviceEventId: e.DeviceEventId,
                    RawMetadata: e.RawMetadata)).ToList();

                try
                {
                    var result = await apiClient.UploadBatchAsync(profile.ApiBaseUrl, profile.AgentIdentifier, payload, ct);
                    await repository.ApplyBatchAcknowledgementAsync(
                        batchId,
                        localIds,
                        result.Accepted,
                        result.Duplicate,
                        result.Rejected,
                        null,
                        ct);

                    batchesSent++;
                    totalAccepted += result.Accepted;
                    totalDuplicate += result.Duplicate;
                    totalRejected += result.Rejected;

                    await repository.WriteHealthLogAsync(
                        "Info",
                        "AttendanceSync",
                        $"Uploaded batch {batchId}: {result.Accepted} accepted, {result.Duplicate} duplicate, {result.Rejected} rejected.",
                        ct: ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    await repository.RecordBatchFailureAsync(batchId, localIds, "Cancelled during upload.", DateTime.UtcNow, ct: CancellationToken.None);
                    throw;
                }
                catch (Exception ex)
                {
                    var maxAttempt = claimed.Max(e => e.RetryCount) + 1;
                    var backoffSec = ComputeBackoffSeconds(maxAttempt, profile.RetryBaseDelaySeconds, profile.RetryMaxDelaySeconds);
                    var nextRetry = DateTime.UtcNow.AddSeconds(backoffSec);

                    await repository.RecordBatchFailureAsync(batchId, localIds, ex.Message, nextRetry, ct: ct);
                    await repository.WriteHealthLogAsync("Warning", "AttendanceSync", $"Batch {batchId} upload failed (retry in {backoffSec}s): {ex.Message}", ct: ct);
                    logger.LogWarning(ex, "Batch {BatchId} upload failed; next retry at {NextRetry}", batchId, nextRetry);

                    return new SyncExecutionSummary(batchesSent, totalAccepted, totalDuplicate, totalRejected, false, ex.Message);
                }
            }

            return new SyncExecutionSummary(batchesSent, totalAccepted, totalDuplicate, totalRejected, true);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public static int ComputeBackoffSeconds(int attempt, int baseDelaySeconds, int maxDelaySeconds)
    {
        var clampedBase = Math.Max(2, baseDelaySeconds);
        var clampedMax = Math.Max(clampedBase, maxDelaySeconds);
        var exp = Math.Min(clampedMax, clampedBase * Math.Pow(2, Math.Min(attempt - 1, 10)));
        var jitter = Random.Shared.NextDouble() * 0.25 * exp;
        return (int)Math.Min(clampedMax, Math.Ceiling(exp + jitter));
    }
}
