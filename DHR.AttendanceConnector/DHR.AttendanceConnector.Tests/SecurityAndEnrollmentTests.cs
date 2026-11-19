using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Services;
using DHR.AttendanceConnector.Devices.Abstractions;
using DHR.AttendanceConnector.Infrastructure.Networking;
using DHR.AttendanceConnector.Infrastructure.Persistence;
using DHR.AttendanceConnector.Infrastructure.Repositories;
using DHR.AttendanceConnector.Infrastructure.Security;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.EdgeAttendance;
using HRM.Services.Attendance.Abstraction;
using HRM.Services.EdgeAttendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using X509CertificateRequest = System.Security.Cryptography.X509Certificates.CertificateRequest;

namespace DHR.AttendanceConnector.Tests;

public sealed class SecurityAndEnrollmentTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "dhr_conn_sec_" + Guid.NewGuid().ToString("N"));
    private readonly string _dbName = "EdgeSecTest_" + Guid.NewGuid().ToString("N");

    private IDbContextFactory<HrmTeContext> CreateDbFactory()
    {
        var opts = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        return new SimpleDbContextFactory(opts);
    }

    [Fact]
    public async Task Enrollment_ProofOfPossession_OneTimeCode_And_ReplayProtection()
    {
        Directory.CreateDirectory(_tempDir);
        var dbFactory = CreateDbFactory();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Organisations.Add(new Organisation { BusinessEntityID = 10, OrganisationName = "Health Ministry" });
            db.AttendanceDevices.Add(new AttendanceDevice { AttendanceDeviceId = 50, Ipaddress = "10.0.0.50", Port = 4370, Location = "Lobby", OrganisationId = 10, AdapterType = "zkteco", IsEdgeEnabled = true });
            db.EdgeAttendanceActivationCodes.Add(new EdgeAttendanceActivationCode
            {
                CodeHash = AttendanceConnectorGatewayService.HashActivationCode("DHR-ACT-ABCD-1234"),
                CodePrefix = "DHR-ACT-ABCD-****",
                OrganisationId = 10,
                SiteName = "Male HQ",
                DeviceIdsJson = "[50]",
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                IsRevoked = false
            });
            await db.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder().Build();
        var ca = new ManagedConnectorCertificateAuthority(dbFactory, config);
        var trust = new DatabaseCertificateTrustValidator(dbFactory);
        var ingestion = new EdgeAttendanceIngestionService(dbFactory, trust, new NoOpLogProcessor());
        var gateway = new AttendanceConnectorGatewayService(dbFactory, trust, ca, ingestion);

        var connOpts = Options.Create(new ConnectorOptions { DataDirectory = _tempDir });
        var keyMgr = new DeviceIdentityKeyManager(new OsProtectedSecretStore(connOpts));

        // 1. Valid proof-of-possession enrollment succeeds
        var (pub, csr, sig, ts) = await keyMgr.GenerateEnrollmentKeyAndProofAsync("DHR-ACT-ABCD-1234", "hq-pc-01");
        var enrolled = await gateway.EnrollAsync(new ConnectorEnrollmentRequest("DHR-ACT-ABCD-1234", "hq-pc-01", "Windows 11", "1.0.0", pub, csr, sig, ts));

        Assert.Equal(10, enrolled.OrganisationId);
        Assert.Equal("Male HQ", enrolled.SiteName);
        Assert.Single(enrolled.Configuration.Devices);
        Assert.Equal(50, enrolled.Configuration.Devices[0].DeviceId);

        // 2. One-time activation code replay is rejected
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            gateway.EnrollAsync(new ConnectorEnrollmentRequest("DHR-ACT-ABCD-1234", "hq-pc-02", "Windows 11", "1.0.0", pub, csr, sig, ts)));

        // 3. Stale replay timestamp is rejected
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.EnrollAsync(new ConnectorEnrollmentRequest("DHR-ACT-ABCD-1234", "hq-pc-01", "Windows 11", "1.0.0", pub, csr, sig, DateTime.UtcNow.AddHours(-1))));

        // 4. Commit issued cert & test cross-tenant / unassigned device rejection
        await keyMgr.CommitIssuedCertificateAsync(enrolled.ClientCertificatePem, enrolled.CaCertificatePem, isRenewal: false);
        using var clientCert = await keyMgr.LoadActiveClientCertificateAsync();
        Assert.NotNull(clientCert);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Organisations.Add(new Organisation { BusinessEntityID = 20, OrganisationName = "Other Ministry" });
            db.AttendanceDevices.Add(new AttendanceDevice { AttendanceDeviceId = 99, Ipaddress = "10.0.1.99", Port = 4370, Location = "Other Org", OrganisationId = 20, AdapterType = "zkteco", IsEdgeEnabled = true });
            await db.SaveChangesAsync();
        }

        // Attempt cross-tenant / unassigned device submission (Device #99 belongs to Org 20)
        var batchRes = await gateway.IngestBatchAsync(clientCert!, new EdgeAttendanceBatchInput(
            enrolled.AgentIdentifier,
            [new EdgeAttendanceEventInput(99, "1001", DateTime.UtcNow.AddMinutes(-1), "CROSS-1", null)]));
        Assert.Equal(0, batchRes.Accepted);
        Assert.Equal(1, batchRes.Rejected);

        // 5. Revoked connector is rejected immediately
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var agent = await db.EdgeAttendanceAgents.SingleAsync(a => a.AgentIdentifier == enrolled.AgentIdentifier);
            agent.IsActive = false;
            agent.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            gateway.GetConfigurationAsync(clientCert!, enrolled.AgentIdentifier));

        // 6. Untrusted / self-signed random certificate is rejected
        using var rogueRsa = RSA.Create(2048);
        var rogueReq = new X509CertificateRequest("CN=rogue", rogueRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var rogueCert = rogueReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            gateway.GetConfigurationAsync(rogueCert, enrolled.AgentIdentifier));
    }

    [Fact]
    public async Task CommandExecution_RejectsUnauthorisedRemoteCommands()
    {
        Directory.CreateDirectory(_tempDir);
        var opts = Options.Create(new ConnectorOptions { DataDirectory = _tempDir });
        var repo = new SqliteConnectorRepository(new SqliteConnectorDatabase(opts));
        await repo.InitializeAsync();

        var keyMgr = new DeviceIdentityKeyManager(new OsProtectedSecretStore(opts));
        var api = new DhrConnectorHttpClient(keyMgr, opts, NullLogger<DhrConnectorHttpClient>.Instance);
        var act = new ConnectorActivationService(repo, keyMgr, api, opts, NullLogger<ConnectorActivationService>.Instance);
        var col = new DeviceCollectionService(repo, new DeviceAdapterRegistry([]), opts, NullLogger<DeviceCollectionService>.Instance);
        var sync = new AttendanceSyncService(repo, api, NullLogger<AttendanceSyncService>.Instance);
        var diag = new DiagnosticsExportService(repo, opts);
        var cmdSvc = new CommandExecutionService(repo, api, act, col, sync, diag, NullLogger<CommandExecutionService>.Instance);

        var (ok1, _, err1) = await cmdSvc.ExecuteSingleCommandAsync(new RemoteCommandItem(1, "ShellExec", "{\"cmd\":\"whoami\"}", DateTime.UtcNow));
        Assert.False(ok1);
        Assert.Contains("unauthorised", err1!, StringComparison.OrdinalIgnoreCase);

        var (ok2, _, err2) = await cmdSvc.ExecuteSingleCommandAsync(new RemoteCommandItem(2, "ScanNetwork", "{\"cidr\":\"192.168.1.0/24\"}", DateTime.UtcNow));
        Assert.False(ok2);
        Assert.Contains("unauthorised", err2!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransportSecurity_RejectsPlainHttpInProduction()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ConnectorActivationService.NormalizeAndValidateUrl("http://dhr.gov.mv/", allowLoopbackHttp: false));

        var ok = ConnectorActivationService.NormalizeAndValidateUrl("https://dhr.gov.mv", allowLoopbackHttp: false);
        Assert.Equal("https://dhr.gov.mv/", ok);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
    }

    private sealed class SimpleDbContextFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
    {
        public HrmTeContext CreateDbContext() => new(options);
    }

    private sealed class NoOpLogProcessor : IAttendanceLogProcessor
    {
        public Task<HRM.Services.Attendance.Processing.AttendanceProcessingResult> ProcessAsync(int attendanceLogId, CancellationToken ct = default) =>
            Task.FromResult(new HRM.Services.Attendance.Processing.AttendanceProcessingResult());
    }
}
