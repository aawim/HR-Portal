using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Core.Services;
using DHR.AttendanceConnector.Devices.Abstractions;
using DHR.AttendanceConnector.Devices.Simulator;
using DHR.AttendanceConnector.Infrastructure.Persistence;
using DHR.AttendanceConnector.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DHR.AttendanceConnector.Tests;

public sealed class AttendanceIntegrationTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "dhr_conn_att_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task EnterpriseScenarios_Official_Overnight_Overlapping_MissingCheckout_ImamFivePrayers_MultiDevice()
    {
        Directory.CreateDirectory(_tempDir);
        var opts = Options.Create(new ConnectorOptions { DataDirectory = _tempDir, EnableDevelopmentSimulator = true });
        var repo = new SqliteConnectorRepository(new SqliteConnectorDatabase(opts));
        await repo.InitializeAsync();

        await repo.SaveProfileAsync(new ConnectorProfile(
            1, "conn-sim-01", 10, "Islamic Affairs", "Main Mosque & HQ", "https://dhr.test/", "THUMB",
            DateTime.UtcNow.AddYears(1), 1, 30, 100, 60, 2, 10, DateTime.UtcNow, DateTime.UtcNow));

        await repo.ReplaceDevicesAsync([
            new DeviceConfiguration(101, "HQ Entrance", "127.0.0.1", 4370, "simulator", 30, true),
            new DeviceConfiguration(102, "Mosque Gate", "127.0.0.1", 4371, "simulator", 30, true)
        ], 1, "hash-1");

        var sim = new VirtualBiometricDeviceSimulator(opts);
        var baseDate = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
        sim.SeedEnterpriseScenarios(101, baseDate);
        sim.SeedLargeBatch(102, 250, baseDate.AddHours(2));

        var collector = new DeviceCollectionService(repo, new DeviceAdapterRegistry([sim]), opts, NullLogger<DeviceCollectionService>.Instance);
        var collected = await collector.PollAllDevicesAsync();

        // Device 101 seeds 20 punches (1 is an exact duplicate => 19 unique). Device 102 seeds 250 unique => 269 total.
        Assert.Equal(269, collected);

        var uploaded = new List<RemoteBatchEventItem>();
        var sink = new CapturingApiClient(uploaded);
        var sync = new AttendanceSyncService(repo, sink, NullLogger<AttendanceSyncService>.Instance);

        var summary = await sync.DrainPendingAsync();
        Assert.True(summary.Succeeded);
        Assert.Equal(3, summary.BatchesSent); // 100 + 100 + 69
        Assert.Equal(269, uploaded.Count);

        // Verify 5 daily Imam prayer-duty sessions (10 punches: 5 check-ins + 5 check-outs) preserved intact
        var imamEvents = uploaded.Where(e => e.LocalUserId == "IMAM-2001").ToList();
        Assert.Equal(10, imamEvents.Count);

        // Verify overnight shift spans midnight UTC
        var overnightEvents = uploaded.Where(e => e.LocalUserId == "EMP-1003").OrderBy(e => e.DeviceTimestamp).ToList();
        Assert.Equal(2, overnightEvents.Count);
        Assert.Equal(baseDate.Date.AddDays(1), overnightEvents[1].DeviceTimestamp.Date);

        // Verify missing checkout preserved without fabrication
        var missingCheckout = uploaded.Where(e => e.LocalUserId == "EMP-1002").ToList();
        Assert.Single(missingCheckout);

        // Verify overlapping official + duty segments (4 punches) preserved
        var overlapping = uploaded.Where(e => e.LocalUserId == "EMP-1004").ToList();
        Assert.Equal(4, overlapping.Count);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
    }

    private sealed class CapturingApiClient(List<RemoteBatchEventItem> captured) : IDhrApiClient
    {
        public Task<RemoteBatchResult> UploadBatchAsync(string apiBaseUrl, string agentId, IReadOnlyList<RemoteBatchEventItem> events, CancellationToken ct = default)
        {
            captured.AddRange(events);
            return Task.FromResult(new RemoteBatchResult(events.Count, 0, 0));
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
