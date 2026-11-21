using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using HRM.Models;
using HRM.Models.EdgeAttendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using X509CertificateRequest = System.Security.Cryptography.X509Certificates.CertificateRequest;

namespace HRM.Services.EdgeAttendance;

public interface IConnectorCertificateAuthority
{
    Task<(string SignedCertificatePem, string CaCertificatePem, string Thumbprint, string Issuer, DateTime NotAfterUtc)> IssueClientCertificateAsync(
        string agentIdentifier,
        string csrPem,
        int validityDays,
        CancellationToken ct = default);
}

public sealed class ManagedConnectorCertificateAuthority(
    IDbContextFactory<HrmTeContext> dbFactory,
    IConfiguration configuration) : IConnectorCertificateAuthority
{
    private readonly SemaphoreSlim _caLock = new(1, 1);
    private X509Certificate2? _caCertificate;

    public async Task<(string SignedCertificatePem, string CaCertificatePem, string Thumbprint, string Issuer, DateTime NotAfterUtc)> IssueClientCertificateAsync(
        string agentIdentifier,
        string csrPem,
        int validityDays,
        CancellationToken ct = default)
    {
        var ca = await GetOrInitializeCaAsync(ct);

        var csr = X509CertificateRequest.LoadSigningRequestPem(
            csrPem,
            HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.SkipSignatureValidation,
            RSASignaturePadding.Pkcs1);

        // Enforce strict subject and EKU (Client Authentication only) regardless of CSR extensions
        var sanitizedCn = new string(agentIdentifier.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        if (string.IsNullOrWhiteSpace(sanitizedCn))
            sanitizedCn = "dhr-connector";

        X509CertificateRequest signedReq;
        using var rsaPub = csr.PublicKey.GetRSAPublicKey();
        using var ecdsaPub = csr.PublicKey.GetECDsaPublicKey();

        if (rsaPub is not null)
        {
            signedReq = new X509CertificateRequest(
                new X500DistinguishedName($"CN={sanitizedCn}, O=DHR Attendance Connector"),
                rsaPub,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        else if (ecdsaPub is not null)
        {
            signedReq = new X509CertificateRequest(
                new X500DistinguishedName($"CN={sanitizedCn}, O=DHR Attendance Connector"),
                ecdsaPub,
                HashAlgorithmName.SHA256);
        }
        else
        {
            throw new InvalidOperationException("Unsupported CSR public key algorithm. Use RSA-2048+ or ECDSA P-256.");
        }

        signedReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        signedReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        signedReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.2") }, false));

        var serial = new byte[16];
        RandomNumberGenerator.Fill(serial);
        serial[0] &= 0x7F; // positive ASN.1 integer

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = DateTimeOffset.UtcNow.AddDays(Math.Clamp(validityDays, 30, 730));

        using var issued = signedReq.Create(ca, notBefore, notAfter, serial);
        var clientPem = issued.ExportCertificatePem();
        var caPem = ca.ExportCertificatePem();

        return (clientPem, caPem, issued.Thumbprint, ca.Subject, issued.NotAfter.ToUniversalTime());
    }

    private async Task<X509Certificate2> GetOrInitializeCaAsync(CancellationToken ct)
    {
        if (_caCertificate is not null)
            return _caCertificate;

        await _caLock.WaitAsync(ct);
        try
        {
            if (_caCertificate is not null)
                return _caCertificate;

            var configuredPath = configuration["AttendanceConnector:CaCertificatePath"];
            var configuredPassword = configuration["AttendanceConnector:CaCertificatePassword"];

            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            {
                _caCertificate = new X509Certificate2(
                    configuredPath,
                    configuredPassword,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
            }
            else
            {
                // Persist a local managed CA PFX in app data for dev/staging zero-touch enrollment.
                var caDir = Path.Combine(AppContext.BaseDirectory, "App_Data", "ConnectorPKI");
                Directory.CreateDirectory(caDir);
                var pfxPath = Path.Combine(caDir, "dhr-connector-ca.pfx");
                var caPassword = configuration["AttendanceConnector:ManagedCaPassword"] ?? "";

                if (File.Exists(pfxPath))
                {
                    _caCertificate = new X509Certificate2(
                        pfxPath,
                        caPassword,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                }
                else
                {
                    using var caRsa = RSA.Create(3072);
                    var caReq = new X509CertificateRequest(
                        "CN=DHR Attendance Connector Root CA, O=Digital HR Maldives",
                        caRsa,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    caReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
                    caReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

                    var created = caReq.CreateSelfSigned(
                        DateTimeOffset.UtcNow.AddMinutes(-5),
                        DateTimeOffset.UtcNow.AddYears(10));

                    var pfxBytes = created.Export(X509ContentType.Pfx, caPassword);
                    await File.WriteAllBytesAsync(pfxPath, pfxBytes, ct);
                    _caCertificate = new X509Certificate2(
                        pfxPath,
                        caPassword,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                }
            }

            // Ensure CA root is registered in EA_TrustedIssuers so DatabaseCertificateTrustValidator trusts it immediately
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = await db.EdgeAttendanceTrustedIssuers
                .FirstOrDefaultAsync(x => x.Thumbprint == _caCertificate.Thumbprint, ct);
            if (existing is null)
            {
                db.EdgeAttendanceTrustedIssuers.Add(new EdgeAttendanceTrustedIssuer
                {
                    Subject = _caCertificate.Subject,
                    Thumbprint = _caCertificate.Thumbprint,
                    CertificateRawData = _caCertificate.RawData,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
            }
            else if (!existing.IsActive || existing.CertificateRawData is null)
            {
                existing.IsActive = true;
                existing.CertificateRawData = _caCertificate.RawData;
                await db.SaveChangesAsync(ct);
            }

            return _caCertificate;
        }
        finally
        {
            _caLock.Release();
        }
    }
}

public sealed record ConnectorEnrollmentRequest(
    string ActivationCode,
    string ConnectorName,
    string OperatingSystem,
    string ConnectorVersion,
    string PublicKeyPem,
    string CsrPem,
    string ProofSignatureBase64,
    DateTime ProofTimestampUtc);

public sealed record ConnectorDeviceConfigDto(
    int DeviceId,
    string Name,
    string IpAddress,
    int Port,
    string ProtocolCode,
    int PollingIntervalSeconds,
    bool Enabled);

public sealed record ConnectorConfigurationResponse(
    int ConnectorId,
    string AgentIdentifier,
    int OrganisationId,
    string OrganisationName,
    string? SiteName,
    int ConfigurationVersion,
    int DefaultPollingIntervalSeconds,
    int SyncBatchSize,
    int HeartbeatIntervalSeconds,
    int RetryBaseDelaySeconds,
    int RetryMaxDelaySeconds,
    IReadOnlyList<ConnectorDeviceConfigDto> Devices);

public sealed record ConnectorEnrollmentResponse(
    int ConnectorId,
    string AgentIdentifier,
    int OrganisationId,
    string OrganisationName,
    string? SiteName,
    string ClientCertificatePem,
    string CaCertificatePem,
    string CertificateThumbprint,
    DateTime CertificateNotAfterUtc,
    ConnectorConfigurationResponse Configuration);

public sealed record ConnectorHeartbeatRequest(
    string AgentId,
    string HealthStatus,
    string? ConnectorVersion,
    string? OperatingSystem,
    int PendingEventCount,
    int ConfigurationVersion,
    DateTime? LastSyncAtUtc);

public sealed record ConnectorCommandPayloadDto(
    long CommandId,
    string CommandType,
    string? PayloadJson,
    DateTime CreatedAtUtc);

public sealed record ConnectorCommandResultRequest(
    string AgentId,
    bool Succeeded,
    string? ResultJson,
    string? ErrorMessage);

public sealed record ConnectorCredentialRenewalRequest(
    string AgentId,
    string NewPublicKeyPem,
    string NewCsrPem,
    string ProofSignatureBase64,
    DateTime ProofTimestampUtc);

public sealed record ConnectorCredentialRenewalResponse(
    string ClientCertificatePem,
    string CaCertificatePem,
    string CertificateThumbprint,
    DateTime CertificateNotAfterUtc);

public sealed class AttendanceConnectorGatewayService(
    IDbContextFactory<HrmTeContext> dbFactory,
    IEdgeCertificateTrustValidator certificateTrust,
    IConnectorCertificateAuthority ca,
    EdgeAttendanceIngestionService ingestionService)
{
    public static readonly HashSet<string> AllowedCommandTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SyncNow",
        "TestDevice",
        "RefreshConfig",
        "ExportDiagnostics"
    };

    public static string HashActivationCode(string plaintextCode)
    {
        var normalized = plaintextCode.Trim().ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    public async Task<ConnectorEnrollmentResponse> EnrollAsync(ConnectorEnrollmentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ActivationCode))
            throw new InvalidOperationException("Activation code is required.");
        if (string.IsNullOrWhiteSpace(request.PublicKeyPem) || string.IsNullOrWhiteSpace(request.CsrPem))
            throw new InvalidOperationException("Public key and CSR are required for proof-of-possession enrollment.");
        if (string.IsNullOrWhiteSpace(request.ProofSignatureBase64))
            throw new InvalidOperationException("Proof-of-possession signature is required.");

        var now = DateTime.UtcNow;
        var skew = (now - request.ProofTimestampUtc.ToUniversalTime()).Duration();
        if (skew > TimeSpan.FromMinutes(10))
            throw new InvalidOperationException("Enrollment proof timestamp is outside the permitted clock skew window.");

        var codeHash = HashActivationCode(request.ActivationCode);
        VerifyProofOfPossession(
            request.PublicKeyPem,
            $"{codeHash}|{request.ConnectorName?.Trim()}|{request.ProofTimestampUtc.ToUniversalTime():O}",
            request.ProofSignatureBase64);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var activation = await db.EdgeAttendanceActivationCodes
            .SingleOrDefaultAsync(x => x.CodeHash == codeHash, ct);

        if (activation is null)
            throw new UnauthorizedAccessException("Invalid activation code.");
        if (activation.IsRevoked)
            throw new UnauthorizedAccessException("Activation code has been revoked.");
        if (activation.UsedAtUtc.HasValue)
            throw new UnauthorizedAccessException("Activation code has already been used.");
        if (activation.ExpiresAtUtc <= now)
            throw new UnauthorizedAccessException("Activation code has expired.");

        var org = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == activation.OrganisationId)
            .Select(o => new { o.BusinessEntityID, o.OrganisationName })
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Target organisation no longer exists.");

        var baseName = !string.IsNullOrWhiteSpace(activation.SuggestedConnectorName)
            ? activation.SuggestedConnectorName.Trim()
            : (!string.IsNullOrWhiteSpace(request.ConnectorName) ? request.ConnectorName.Trim() : $"conn-{activation.OrganisationId}");

        var sanitized = new string(baseName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = $"conn-{activation.OrganisationId}";

        var agentIdentifier = sanitized;
        if (await db.EdgeAttendanceAgents.AnyAsync(a => a.AgentIdentifier == agentIdentifier, ct))
        {
            agentIdentifier = $"{sanitized}-{RandomNumberGenerator.GetHexString(6).ToLowerInvariant()}";
        }

        var (clientPem, caPem, thumbprint, issuer, notAfterUtc) = await ca.IssueClientCertificateAsync(
            agentIdentifier,
            request.CsrPem,
            validityDays: 365,
            ct);

        var agent = new EdgeAttendanceAgent
        {
            AgentIdentifier = agentIdentifier,
            OrganisationId = activation.OrganisationId,
            SiteName = activation.SiteName,
            CertificateThumbprint = thumbprint,
            CertificateIssuer = issuer,
            CertificateNotAfterUtc = notAfterUtc,
            IsActive = true,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            LastHealthStatus = "Online",
            ConnectorVersion = request.ConnectorVersion?.Trim(),
            OperatingSystem = request.OperatingSystem?.Trim(),
            ConfigurationVersion = 1,
            PendingEventCount = 0,
            PublicKeyPem = request.PublicKeyPem.Trim()
        };

        db.EdgeAttendanceAgents.Add(agent);
        await db.SaveChangesAsync(ct);

        // Bind pre-selected devices for this activation code (strictly tenant-scoped)
        if (!string.IsNullOrWhiteSpace(activation.DeviceIdsJson))
        {
            var deviceIds = JsonSerializer.Deserialize<List<int>>(activation.DeviceIdsJson) ?? [];
            if (deviceIds.Count > 0)
            {
                var devices = await db.AttendanceDevices
                    .Where(d => deviceIds.Contains(d.AttendanceDeviceId) && d.OrganisationId == activation.OrganisationId)
                    .ToListAsync(ct);

                foreach (var device in devices)
                {
                    device.EdgeAttendanceAgentId = agent.EdgeAttendanceAgentId;
                    device.IsEdgeEnabled = true;

                    var existingBinding = await db.EdgeAttendanceAgentDevices
                        .FirstOrDefaultAsync(b => b.EdgeAttendanceAgentId == agent.EdgeAttendanceAgentId && b.AttendanceDeviceId == device.AttendanceDeviceId, ct);
                    if (existingBinding is null)
                    {
                        db.EdgeAttendanceAgentDevices.Add(new EdgeAttendanceAgentDevice
                        {
                            EdgeAttendanceAgentId = agent.EdgeAttendanceAgentId,
                            AttendanceDeviceId = device.AttendanceDeviceId,
                            IsActive = true,
                            CreatedAtUtc = now,
                            PollingIntervalSeconds = 30
                        });
                    }
                    else
                    {
                        existingBinding.IsActive = true;
                    }
                }
            }
        }

        activation.UsedAtUtc = now;
        activation.UsedByEdgeAttendanceAgentId = agent.EdgeAttendanceAgentId;
        await db.SaveChangesAsync(ct);

        var config = await BuildConfigurationAsync(db, agent, org.OrganisationName, ct);

        return new ConnectorEnrollmentResponse(
            agent.EdgeAttendanceAgentId,
            agent.AgentIdentifier,
            agent.OrganisationId,
            org.OrganisationName,
            agent.SiteName,
            clientPem,
            caPem,
            thumbprint,
            notAfterUtc,
            config);
    }

    public async Task<ConnectorConfigurationResponse> GetConfigurationAsync(
        X509Certificate2 certificate,
        string agentId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var agent = await AuthenticateAsync(db, certificate, agentId, ct);
        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == agent.OrganisationId)
            .Select(o => o.OrganisationName)
            .SingleOrDefaultAsync(ct) ?? $"Organisation #{agent.OrganisationId}";

        return await BuildConfigurationAsync(db, agent, orgName, ct);
    }

    public async Task<EdgeAttendanceIngestionResult> IngestBatchAsync(
        X509Certificate2 certificate,
        EdgeAttendanceBatchInput batch,
        CancellationToken ct = default)
    {
        var result = await ingestionService.AcceptAsync(certificate, batch, ct);
        if (result.Accepted > 0 || result.Duplicate > 0)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var agent = await db.EdgeAttendanceAgents
                .SingleOrDefaultAsync(x => x.AgentIdentifier == batch.AgentId.Trim(), ct);
            if (agent is not null)
            {
                agent.LastSyncAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
        return result;
    }

    public async Task HeartbeatAsync(
        X509Certificate2 certificate,
        ConnectorHeartbeatRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.HealthStatus) ||
            (request.HealthStatus != "Online" && request.HealthStatus != "Degraded" && request.HealthStatus != "SyncDelayed"))
        {
            throw new InvalidOperationException("Unsupported health status.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var agent = await AuthenticateAsync(db, certificate, request.AgentId, ct);
        agent.LastSeenAtUtc = DateTime.UtcNow;
        agent.LastHealthStatus = request.HealthStatus;
        if (!string.IsNullOrWhiteSpace(request.ConnectorVersion))
            agent.ConnectorVersion = request.ConnectorVersion.Trim();
        if (!string.IsNullOrWhiteSpace(request.OperatingSystem))
            agent.OperatingSystem = request.OperatingSystem.Trim();
        agent.PendingEventCount = Math.Max(0, request.PendingEventCount);
        if (request.LastSyncAtUtc.HasValue)
            agent.LastSyncAtUtc = request.LastSyncAtUtc.Value.ToUniversalTime();
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ConnectorCommandPayloadDto>> GetPendingCommandsAsync(
        X509Certificate2 certificate,
        string agentId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var agent = await AuthenticateAsync(db, certificate, agentId, ct);

        var pending = await db.EdgeAttendanceConnectorCommands
            .Where(c => c.EdgeAttendanceAgentId == agent.EdgeAttendanceAgentId && c.Status == "Pending")
            .OrderBy(c => c.CreatedAtUtc)
            .Take(20)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var cmd in pending)
        {
            cmd.Status = "Dispatched";
            cmd.DispatchedAtUtc = now;
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);

        return pending
            .Where(c => AllowedCommandTypes.Contains(c.CommandType))
            .Select(c => new ConnectorCommandPayloadDto(c.EdgeAttendanceConnectorCommandId, c.CommandType, c.PayloadJson, c.CreatedAtUtc))
            .ToList();
    }

    public async Task ReportCommandResultAsync(
        X509Certificate2 certificate,
        long commandId,
        ConnectorCommandResultRequest request,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var agent = await AuthenticateAsync(db, certificate, request.AgentId, ct);

        var cmd = await db.EdgeAttendanceConnectorCommands
            .SingleOrDefaultAsync(c => c.EdgeAttendanceConnectorCommandId == commandId && c.EdgeAttendanceAgentId == agent.EdgeAttendanceAgentId, ct)
            ?? throw new InvalidOperationException("Command not found for this connector.");

        cmd.Status = request.Succeeded ? "Completed" : "Failed";
        cmd.CompletedAtUtc = DateTime.UtcNow;
        cmd.ResultJson = request.ResultJson?.Length > 4000 ? request.ResultJson[..4000] : request.ResultJson;
        cmd.ErrorMessage = request.ErrorMessage?.Length > 1000 ? request.ErrorMessage[..1000] : request.ErrorMessage;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ConnectorCredentialRenewalResponse> RenewCredentialsAsync(
        X509Certificate2 certificate,
        ConnectorCredentialRenewalRequest request,
        CancellationToken ct = default)
    {
        var skew = (DateTime.UtcNow - request.ProofTimestampUtc.ToUniversalTime()).Duration();
        if (skew > TimeSpan.FromMinutes(10))
            throw new InvalidOperationException("Renewal proof timestamp is outside the permitted clock skew window.");

        VerifyProofOfPossession(
            request.NewPublicKeyPem,
            $"RENEW|{request.AgentId.Trim()}|{request.ProofTimestampUtc.ToUniversalTime():O}",
            request.ProofSignatureBase64);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var agent = await AuthenticateAsync(db, certificate, request.AgentId, ct);

        var (clientPem, caPem, thumbprint, issuer, notAfterUtc) = await ca.IssueClientCertificateAsync(
            agent.AgentIdentifier,
            request.NewCsrPem,
            validityDays: 365,
            ct);

        agent.CertificateThumbprint = thumbprint;
        agent.CertificateIssuer = issuer;
        agent.CertificateNotAfterUtc = notAfterUtc;
        agent.PublicKeyPem = request.NewPublicKeyPem.Trim();
        agent.LastSeenAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new ConnectorCredentialRenewalResponse(clientPem, caPem, thumbprint, notAfterUtc);
    }

    private async Task<EdgeAttendanceAgent> AuthenticateAsync(
        HrmTeContext db,
        X509Certificate2 certificate,
        string agentId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new UnauthorizedAccessException("Agent identity is required.");

        await certificateTrust.ValidateAsync(certificate, ct);
        var thumbprint = certificate.Thumbprint?.Trim();
        if (string.IsNullOrWhiteSpace(thumbprint))
            throw new UnauthorizedAccessException("Client certificate has no thumbprint.");

        return await db.EdgeAttendanceAgents.SingleOrDefaultAsync(
            x => x.AgentIdentifier == agentId.Trim()
                 && x.CertificateThumbprint == thumbprint
                 && x.IsActive
                 && x.RevokedAtUtc == null
                 && x.CertificateNotAfterUtc > DateTime.UtcNow,
            ct) ?? throw new UnauthorizedAccessException("Connector is not active or has been revoked.");
    }

    private static async Task<ConnectorConfigurationResponse> BuildConfigurationAsync(
        HrmTeContext db,
        EdgeAttendanceAgent agent,
        string organisationName,
        CancellationToken ct)
    {
        var boundDevices = await (from b in db.EdgeAttendanceAgentDevices.AsNoTracking()
                                  join d in db.AttendanceDevices.AsNoTracking() on b.AttendanceDeviceId equals d.AttendanceDeviceId
                                  where b.EdgeAttendanceAgentId == agent.EdgeAttendanceAgentId
                                        && b.IsActive
                                        && d.OrganisationId == agent.OrganisationId
                                        && d.IsEdgeEnabled
                                  orderby d.Location, d.AttendanceDeviceId
                                  select new ConnectorDeviceConfigDto(
                                      d.AttendanceDeviceId,
                                      string.IsNullOrWhiteSpace(d.Location) ? $"Device #{d.AttendanceDeviceId}" : d.Location,
                                      d.Ipaddress,
                                      d.Port > 0 ? d.Port : 4370,
                                      string.IsNullOrWhiteSpace(d.AdapterType) ? "zkteco" : d.AdapterType,
                                      b.PollingIntervalSeconds > 0 ? b.PollingIntervalSeconds : 30,
                                      d.IsEdgeEnabled))
            .ToListAsync(ct);

        return new ConnectorConfigurationResponse(
            agent.EdgeAttendanceAgentId,
            agent.AgentIdentifier,
            agent.OrganisationId,
            organisationName,
            agent.SiteName,
            agent.ConfigurationVersion,
            DefaultPollingIntervalSeconds: 30,
            SyncBatchSize: 200,
            HeartbeatIntervalSeconds: 60,
            RetryBaseDelaySeconds: 5,
            RetryMaxDelaySeconds: 300,
            Devices: boundDevices);
    }

    public static void VerifyProofOfPossession(string publicKeyPem, string canonicalMessage, string signatureBase64)
    {
        var data = Encoding.UTF8.GetBytes(canonicalMessage);
        var signature = Convert.FromBase64String(signatureBase64.Trim());

        if (publicKeyPem.Contains("PUBLIC KEY", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);
                if (rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    return;
            }
            catch
            {
                // Fallback to ECDSA
            }

            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(publicKeyPem);
                if (ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256))
                    return;
            }
            catch
            {
                // Fall through to throw
            }
        }

        throw new UnauthorizedAccessException("Proof-of-possession signature verification failed.");
    }
}
