using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Core.Services;

public sealed class ConnectorActivationService(
    IConnectorRepository repository,
    IDeviceIdentityKeyManager keyManager,
    IDhrApiClient apiClient,
    IOptions<ConnectorOptions> options,
    ILogger<ConnectorActivationService> logger)
{
    public async Task<ConnectorProfile> ActivateAsync(
        string apiBaseUrl,
        string activationCode,
        string? connectorName = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(activationCode))
            throw new InvalidOperationException("Activation code is required.");

        var normalizedUrl = NormalizeAndValidateUrl(apiBaseUrl, options.Value.AllowLoopbackHttpForDevelopment);
        var hostName = string.IsNullOrWhiteSpace(connectorName) ? Environment.MachineName : connectorName.Trim();
        var osDesc = $"{Environment.OSVersion.Platform} {System.Runtime.InteropServices.RuntimeInformation.OSDescription}".Trim();

        await repository.InitializeAsync(ct);

        var (pubPem, csrPem, proofSig, proofTime) = await keyManager.GenerateEnrollmentKeyAndProofAsync(
            activationCode,
            hostName,
            ct);

        var result = await apiClient.EnrollAsync(
            normalizedUrl,
            new EnrollConnectorPayload(
                ActivationCode: activationCode.Trim(),
                ConnectorName: hostName,
                OperatingSystem: osDesc,
                ConnectorVersion: options.Value.ConnectorVersion,
                PublicKeyPem: pubPem,
                CsrPem: csrPem,
                ProofSignatureBase64: proofSig,
                ProofTimestampUtc: proofTime),
            ct);

        await keyManager.CommitIssuedCertificateAsync(result.ClientCertificatePem, result.CaCertificatePem, isRenewal: false, ct);

        var now = DateTime.UtcNow;
        var profile = new ConnectorProfile(
            ConnectorId: result.ConnectorId,
            AgentIdentifier: result.AgentIdentifier,
            OrganisationId: result.OrganisationId,
            OrganisationName: result.OrganisationName,
            SiteName: result.SiteName,
            ApiBaseUrl: normalizedUrl,
            CertificateThumbprint: result.CertificateThumbprint,
            CertificateNotAfterUtc: result.CertificateNotAfterUtc,
            ConfigurationVersion: result.Configuration.ConfigurationVersion,
            DefaultPollingIntervalSeconds: result.Configuration.DefaultPollingIntervalSeconds,
            SyncBatchSize: result.Configuration.SyncBatchSize,
            HeartbeatIntervalSeconds: result.Configuration.HeartbeatIntervalSeconds,
            RetryBaseDelaySeconds: result.Configuration.RetryBaseDelaySeconds,
            RetryMaxDelaySeconds: result.Configuration.RetryMaxDelaySeconds,
            ActivatedAtUtc: now,
            LastConfigSyncAtUtc: now);

        await repository.SaveProfileAsync(profile, ct);
        await ApplyRemoteConfigurationAsync(result.Configuration, ct);
        await repository.WriteHealthLogAsync("Info", "Activation", $"Connector activated as '{profile.AgentIdentifier}' for {profile.OrganisationName}.", ct: ct);

        logger.LogInformation("Connector activated successfully as {AgentId} for org {OrgId}", profile.AgentIdentifier, profile.OrganisationId);
        return profile;
    }

    public async Task<ConnectorProfile?> RefreshConfigurationAsync(CancellationToken ct = default)
    {
        var profile = await repository.GetProfileAsync(ct);
        if (profile is null) return null;

        var remote = await apiClient.GetConfigurationAsync(profile.ApiBaseUrl, profile.AgentIdentifier, ct);

        // Security guard: connector cannot change its own tenant association silently
        if (remote.OrganisationId != profile.OrganisationId)
            throw new UnauthorizedAccessException("Configuration tenant mismatch rejected by connector.");

        var updated = profile with
        {
            OrganisationName = remote.OrganisationName,
            SiteName = remote.SiteName,
            ConfigurationVersion = remote.ConfigurationVersion,
            DefaultPollingIntervalSeconds = remote.DefaultPollingIntervalSeconds,
            SyncBatchSize = remote.SyncBatchSize,
            HeartbeatIntervalSeconds = remote.HeartbeatIntervalSeconds,
            RetryBaseDelaySeconds = remote.RetryBaseDelaySeconds,
            RetryMaxDelaySeconds = remote.RetryMaxDelaySeconds,
            LastConfigSyncAtUtc = DateTime.UtcNow
        };

        await repository.SaveProfileAsync(updated, ct);
        await ApplyRemoteConfigurationAsync(remote, ct);
        await repository.WriteHealthLogAsync("Info", "ConfigSync", $"Applied configuration version {remote.ConfigurationVersion} ({remote.Devices.Count} device(s)).", ct: ct);
        return updated;
    }

    public async Task<ConnectorProfile?> RenewCredentialsIfNeededAsync(bool force = false, CancellationToken ct = default)
    {
        var profile = await repository.GetProfileAsync(ct);
        if (profile is null) return null;

        if (!force && profile.CertificateNotAfterUtc > DateTime.UtcNow.AddDays(30))
            return profile;

        var (newPub, newCsr, proofSig, proofTime) = await keyManager.GenerateRenewalKeyAndProofAsync(profile.AgentIdentifier, ct);
        var renewed = await apiClient.RenewCredentialsAsync(
            profile.ApiBaseUrl,
            profile.AgentIdentifier,
            newPub,
            newCsr,
            proofSig,
            proofTime,
            ct);

        await keyManager.CommitIssuedCertificateAsync(renewed.ClientCertificatePem, renewed.CaCertificatePem, isRenewal: true, ct);

        var updated = profile with
        {
            CertificateThumbprint = renewed.CertificateThumbprint,
            CertificateNotAfterUtc = renewed.CertificateNotAfterUtc
        };
        await repository.SaveProfileAsync(updated, ct);
        await repository.WriteHealthLogAsync("Info", "Security", $"Client certificate rotated. New expiry: {updated.CertificateNotAfterUtc:yyyy-MM-dd}.", ct: ct);
        return updated;
    }

    private async Task ApplyRemoteConfigurationAsync(RemoteConnectorConfiguration remote, CancellationToken ct)
    {
        var devices = remote.Devices.Select(d => new DeviceConfiguration(
            d.DeviceId,
            d.Name,
            d.IpAddress,
            d.Port,
            d.ProtocolCode,
            d.PollingIntervalSeconds,
            d.Enabled)).ToList();

        var json = JsonSerializer.Serialize(devices);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        await repository.ReplaceDevicesAsync(devices, remote.ConfigurationVersion, hash, ct);
    }

    public static string NormalizeAndValidateUrl(string url, bool allowLoopbackHttp)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            throw new InvalidOperationException("DHR API URL must be a valid absolute URI.");

        if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return uri.ToString().TrimEnd('/') + "/";

        if (allowLoopbackHttp && uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && uri.IsLoopback)
            return uri.ToString().TrimEnd('/') + "/";

        throw new InvalidOperationException("HTTPS is mandatory for DHR API communication.");
    }
}
