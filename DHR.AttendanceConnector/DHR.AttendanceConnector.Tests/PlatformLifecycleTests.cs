using System.IO.Compression;
using System.Text;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Core.Services;
using DHR.AttendanceConnector.Devices.Abstractions;
using DHR.AttendanceConnector.Devices.Simulator;
using DHR.AttendanceConnector.Infrastructure.Persistence;
using DHR.AttendanceConnector.Infrastructure.Repositories;
using DHR.AttendanceConnector.Infrastructure.Security;
using DHR.AttendanceConnector.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DHR.AttendanceConnector.Tests;

public sealed class PlatformLifecycleTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "dhr_conn_plat_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task BackgroundWorkers_StartAndStopGracefullyWithoutGui()
    {
        Directory.CreateDirectory(_tempDir);
        var opts = Options.Create(new ConnectorOptions { DataDirectory = _tempDir, EnableDevelopmentSimulator = true });
        var repo = new SqliteConnectorRepository(new SqliteConnectorDatabase(opts));
        await repo.InitializeAsync();

        var sim = new VirtualBiometricDeviceSimulator(opts);
        var collector = new DeviceCollectionService(repo, new DeviceAdapterRegistry([sim]), opts, NullLogger<DeviceCollectionService>.Instance);
        var pollingWorker = new DevicePollingWorker(repo, collector, NullLogger<DevicePollingWorker>.Instance);

        using var cts = new CancellationTokenSource();
        await pollingWorker.StartAsync(cts.Token);
        await Task.Delay(150);
        cts.Cancel();
        await pollingWorker.StopAsync(CancellationToken.None);

        Assert.True(await repo.CheckHealthAsync());
    }

    [Fact]
    public async Task SecretStore_EncryptsAtRest_And_DiagnosticsNeverLeakSecrets()
    {
        Directory.CreateDirectory(_tempDir);
        var opts = Options.Create(new ConnectorOptions { DataDirectory = _tempDir });
        var store = new OsProtectedSecretStore(opts);
        var secretText = "-----BEGIN PRIVATE KEY-----\nSUPER_SECRET_MATERIAL\n-----END PRIVATE KEY-----";
        await store.SaveSecretAsync("test.key", Encoding.UTF8.GetBytes(secretText));

        // Verify raw file on disk does not contain plaintext private key
        var files = Directory.GetFiles(Path.Combine(_tempDir, "secrets"));
        Assert.NotEmpty(files);
        foreach (var f in files)
        {
            var raw = await File.ReadAllTextAsync(f);
            Assert.DoesNotContain("SUPER_SECRET_MATERIAL", raw);
        }

        // Verify decryption round-trip
        var loaded = await store.LoadSecretAsync("test.key");
        Assert.Equal(secretText, Encoding.UTF8.GetString(loaded!));

        // Verify diagnostics export contains no private key material
        var repo = new SqliteConnectorRepository(new SqliteConnectorDatabase(opts));
        await repo.InitializeAsync();
        await repo.SaveProfileAsync(new ConnectorProfile(
            1, "conn-diag", 10, "Test Org", "Site A", "https://dhr.gov.mv/", "AABBCCDDEEFF001122334455",
            DateTime.UtcNow.AddYears(1), 2, 30, 200, 60, 5, 300, DateTime.UtcNow, DateTime.UtcNow));

        var diagSvc = new DiagnosticsExportService(repo, opts);
        var zipPath = await diagSvc.ExportSanitisedZipAsync(_tempDir);
        Assert.True(File.Exists(zipPath));

        using var zip = ZipFile.OpenRead(zipPath);
        var entry = zip.GetEntry("diagnostics-summary.json");
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry!.Open());
        var content = await reader.ReadToEndAsync();
        Assert.DoesNotContain("PRIVATE KEY", content);
        Assert.DoesNotContain("AABBCCDDEEFF001122334455", content); // full thumbprint redacted to prefix
        Assert.Contains("AABBCCDD...", content);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
    }
}
