using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Core.Services;
using DHR.AttendanceConnector.Infrastructure.Persistence;
using DHR.AttendanceConnector.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DHR.AttendanceConnector.Tests;

public sealed class DataIntegrityTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "dhr_conn_di_" + Guid.NewGuid().ToString("N"));

    private SqliteConnectorRepository CreateRepo()
    {
        Directory.CreateDirectory(_tempDir);
        var opts = Options.Create(new ConnectorOptions { DataDirectory = _tempDir });
        return new SqliteConnectorRepository(new SqliteConnectorDatabase(opts));
    }

    [Fact]
    public async Task DuplicateEvents_AreDeduplicatedInSqliteOutbox()
    {
        var repo = CreateRepo();
        await repo.InitializeAsync();

        var ts = new DateTime(2026, 11, 19, 3, 15, 0, DateTimeKind.Utc);
        var key = DeviceCollectionService.ComputeDeduplicationKey("conn-1", 10, "U100", ts, "EVT-1");
        var ev = new RawAttendanceEvent(0, "conn-1", 10, "U100", ts, "EVT-1", "0", "zkteco", null, DateTime.UtcNow, key);

        var first = await repo.PersistCollectedEventsAsync([ev]);
        var second = await repo.PersistCollectedEventsAsync([ev]);

        Assert.Equal(1, first);
        Assert.Equal(0, second);

        var metrics = await repo.GetSyncMetricsAsync();
        Assert.Equal(1, metrics.PendingCount);
    }

    [Fact]
    public async Task CrashRecovery_RestoresStaleUploadingEventsAndCheckpointsPersistAcrossRestart()
    {
        var repo1 = CreateRepo();
        await repo1.InitializeAsync();

        var ts = DateTime.UtcNow.AddMinutes(-5);
        await repo1.SaveCheckpointAsync(new DeviceSyncCheckpoint(10, "EVT-99", ts, 99, DateTime.UtcNow));
        var key = DeviceCollectionService.ComputeDeduplicationKey("conn-1", 10, "U200", ts, "EVT-99");
        await repo1.PersistCollectedEventsAsync([new RawAttendanceEvent(0, "conn-1", 10, "U200", ts, "EVT-99", "0", "zkteco", null, DateTime.UtcNow, key)]);

        // Claim batch to simulate mid-upload crash
        var claimed = await repo1.ClaimPendingBatchAsync(50, "crash-batch-1");
        Assert.Single(claimed);
        Assert.Equal(1, (await repo1.GetSyncMetricsAsync()).UploadingCount);

        // Simulate fresh process startup with new repository instance
        var repo2 = CreateRepo();
        await repo2.InitializeAsync();
        await repo2.RecoverStaleUploadingEventsAsync(TimeSpan.Zero);

        var recoveredMetrics = await repo2.GetSyncMetricsAsync();
        Assert.Equal(1, recoveredMetrics.PendingCount);
        Assert.Equal(0, recoveredMetrics.UploadingCount);

        var cp = await repo2.GetCheckpointAsync(10);
        Assert.NotNull(cp);
        Assert.Equal("EVT-99", cp!.LastEventId);
        Assert.Equal(99, cp.TotalEventsCollected);
    }

    [Fact]
    public async Task InterruptedUpload_RetriesAndThenAcknowledgesPartialBatch()
    {
        var repo = CreateRepo();
        await repo.InitializeAsync();

        await repo.SaveProfileAsync(new ConnectorProfile(
            1, "conn-1", 10, "Ministry of Finance", "HQ", "https://dhr.test/", "THUMB", DateTime.UtcNow.AddYears(1),
            1, 30, 50, 60, 2, 10, DateTime.UtcNow, DateTime.UtcNow));

        var ts = DateTime.UtcNow.AddMinutes(-2);
        var events = Enumerable.Range(1, 5).Select(i =>
        {
            var key = DeviceCollectionService.ComputeDeduplicationKey("conn-1", 10, $"U{i}", ts.AddSeconds(i), $"E-{i}");
            return new RawAttendanceEvent(0, "conn-1", 10, $"U{i}", ts.AddSeconds(i), $"E-{i}", "0", "zkteco", null, DateTime.UtcNow, key);
        }).ToList();
        await repo.PersistCollectedEventsAsync(events);

        var fakeClient = new FlakyDhrApiClient(failFirstAttempt: true, accepted: 3, duplicate: 1, rejected: 1);
        var sync = new AttendanceSyncService(repo, fakeClient, NullLogger<AttendanceSyncService>.Instance);

        // Attempt 1: simulated network interruption
        var firstAttempt = await sync.DrainPendingAsync();
        Assert.False(firstAttempt.Succeeded);
        Assert.Equal(5, (await repo.GetSyncMetricsAsync()).PendingCount);

        // Force retry window open and attempt 2: partial acceptance (3 accepted, 1 duplicate, 1 rejected)
        await repo.RetryPendingAndQuarantinedAsync();
        var secondAttempt = await sync.DrainPendingAsync();
        Assert.True(secondAttempt.Succeeded);
        Assert.Equal(3, secondAttempt.Accepted);
        Assert.Equal(1, secondAttempt.Duplicate);
        Assert.Equal(1, secondAttempt.Rejected);
        Assert.Equal(0, (await repo.GetSyncMetricsAsync()).PendingCount);
        Assert.Equal(5, (await repo.GetSyncMetricsAsync()).AcknowledgedCount);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
    }

    private sealed class FlakyDhrApiClient(bool failFirstAttempt, int accepted, int duplicate, int rejected) : IDhrApiClient
    {
        private int _calls;

        public Task<RemoteBatchResult> UploadBatchAsync(string apiBaseUrl, string agentId, IReadOnlyList<RemoteBatchEventItem> events, CancellationToken ct = default)
        {
            _calls++;
            if (failFirstAttempt && _calls == 1)
                throw new HttpRequestException("Simulated network outage");
            return Task.FromResult(new RemoteBatchResult(accepted, duplicate, rejected));
        }

        public Task<RemoteEnrollmentResult> EnrollAsync(string apiBaseUrl, EnrollConnectorPayload payload, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RemoteConnectorConfiguration> GetConfigurationAsync(string apiBaseUrl, string agentId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task SendHeartbeatAsync(string apiBaseUrl, RemoteHeartbeatPayload payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<RemoteCommandItem>> PollCommandsAsync(string apiBaseUrl, string agentId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RemoteCommandItem>>([]);
        public Task ReportCommandResultAsync(string apiBaseUrl, long commandId, string agentId, bool succeeded, string? resultJson, string? errorMessage, CancellationToken ct = default) => Task.CompletedTask;
        public Task<RemoteRenewalResult> RenewCredentialsAsync(string apiBaseUrl, string agentId, string newPublicKeyPem, string newCsrPem, string proofSignatureBase64, DateTime proofTimestampUtc, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(bool Reachable, string Message)> TestConnectionAsync(string apiBaseUrl, string? agentId, CancellationToken ct = default) => Task.FromResult((true, "OK"));
    }
}
