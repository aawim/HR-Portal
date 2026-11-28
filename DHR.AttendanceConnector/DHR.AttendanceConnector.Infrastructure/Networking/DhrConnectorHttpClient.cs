using System.Net.Http.Json;
using System.Security.Authentication;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Infrastructure.Networking;

/// <summary>
/// Outbound-only HTTPS/mTLS client for communicating with the central DHR Attendance Gateway.
/// Enforces TLS 1.2/1.3 and never disables server certificate validation.
/// </summary>
public sealed class DhrConnectorHttpClient(
    IDeviceIdentityKeyManager keyManager,
    IOptions<ConnectorOptions> options,
    ILogger<DhrConnectorHttpClient> logger) : IDhrApiClient
{
    public async Task<RemoteEnrollmentResult> EnrollAsync(string apiBaseUrl, EnrollConnectorPayload payload, CancellationToken ct = default)
    {
        using var client = CreateHttpClient(apiBaseUrl, clientCertificate: null);
        using var response = await client.PostAsJsonAsync("api/attendance/connectors/enroll", payload, ct);
        await EnsureSuccessAsync(response, "Connector enrollment", ct);
        return (await response.Content.ReadFromJsonAsync<RemoteEnrollmentResult>(cancellationToken: ct))
            ?? throw new InvalidOperationException("Empty enrollment response from DHR.");
    }

    public async Task<RemoteConnectorConfiguration> GetConfigurationAsync(string apiBaseUrl, string agentId, CancellationToken ct = default)
    {
        using var cert = await RequireClientCertificateAsync(ct);
        using var client = CreateHttpClient(apiBaseUrl, cert);
        using var response = await client.GetAsync($"api/attendance/connectors/configuration?agentId={Uri.EscapeDataString(agentId)}", ct);
        await EnsureSuccessAsync(response, "Configuration sync", ct);
        return (await response.Content.ReadFromJsonAsync<RemoteConnectorConfiguration>(cancellationToken: ct))
            ?? throw new InvalidOperationException("Empty configuration response from DHR.");
    }

    public async Task<RemoteBatchResult> UploadBatchAsync(string apiBaseUrl, string agentId, IReadOnlyList<RemoteBatchEventItem> events, CancellationToken ct = default)
    {
        using var cert = await RequireClientCertificateAsync(ct);
        using var client = CreateHttpClient(apiBaseUrl, cert);
        using var response = await client.PostAsJsonAsync("api/attendance/connectors/events/batch", new { AgentId = agentId, Events = events }, ct);
        await EnsureSuccessAsync(response, "Attendance batch upload", ct);
        return (await response.Content.ReadFromJsonAsync<RemoteBatchResult>(cancellationToken: ct))
            ?? throw new InvalidOperationException("Empty batch ingestion response from DHR.");
    }

    public async Task SendHeartbeatAsync(string apiBaseUrl, RemoteHeartbeatPayload payload, CancellationToken ct = default)
    {
        using var cert = await RequireClientCertificateAsync(ct);
        using var client = CreateHttpClient(apiBaseUrl, cert);
        using var response = await client.PostAsJsonAsync("api/attendance/connectors/heartbeat", payload, ct);
        await EnsureSuccessAsync(response, "Connector heartbeat", ct);
    }

    public async Task<IReadOnlyList<RemoteCommandItem>> PollCommandsAsync(string apiBaseUrl, string agentId, CancellationToken ct = default)
    {
        using var cert = await RequireClientCertificateAsync(ct);
        using var client = CreateHttpClient(apiBaseUrl, cert);
        using var response = await client.GetAsync($"api/attendance/connectors/commands?agentId={Uri.EscapeDataString(agentId)}", ct);
        await EnsureSuccessAsync(response, "Command poll", ct);
        return (await response.Content.ReadFromJsonAsync<List<RemoteCommandItem>>(cancellationToken: ct)) ?? [];
    }

    public async Task ReportCommandResultAsync(string apiBaseUrl, long commandId, string agentId, bool succeeded, string? resultJson, string? errorMessage, CancellationToken ct = default)
    {
        using var cert = await RequireClientCertificateAsync(ct);
        using var client = CreateHttpClient(apiBaseUrl, cert);
        using var response = await client.PostAsJsonAsync(
            $"api/attendance/connectors/commands/{commandId}/result",
            new { AgentId = agentId, Succeeded = succeeded, ResultJson = resultJson, ErrorMessage = errorMessage },
            ct);
        await EnsureSuccessAsync(response, "Command result report", ct);
    }

    public async Task<RemoteRenewalResult> RenewCredentialsAsync(
        string apiBaseUrl,
        string agentId,
        string newPublicKeyPem,
        string newCsrPem,
        string proofSignatureBase64,
        DateTime proofTimestampUtc,
        CancellationToken ct = default)
    {
        using var cert = await RequireClientCertificateAsync(ct);
        using var client = CreateHttpClient(apiBaseUrl, cert);
        using var response = await client.PostAsJsonAsync(
            "api/attendance/connectors/credentials/renew",
            new
            {
                AgentId = agentId,
                NewPublicKeyPem = newPublicKeyPem,
                NewCsrPem = newCsrPem,
                ProofSignatureBase64 = proofSignatureBase64,
                ProofTimestampUtc = proofTimestampUtc
            },
            ct);
        await EnsureSuccessAsync(response, "Credential renewal", ct);
        return (await response.Content.ReadFromJsonAsync<RemoteRenewalResult>(cancellationToken: ct))
            ?? throw new InvalidOperationException("Empty credential renewal response from DHR.");
    }

    public async Task<(bool Reachable, string Message)> TestConnectionAsync(string apiBaseUrl, string? agentId, CancellationToken ct = default)
    {
        try
        {
            var cert = await keyManager.LoadActiveClientCertificateAsync(ct);
            using (cert)
            using (var client = CreateHttpClient(apiBaseUrl, cert))
            {
                if (!string.IsNullOrWhiteSpace(agentId) && cert is not null)
                {
                    using var resp = await client.GetAsync($"api/attendance/connectors/configuration?agentId={Uri.EscapeDataString(agentId)}", ct);
                    if (resp.IsSuccessStatusCode)
                        return (true, "Authenticated mTLS connection to DHR succeeded.");
                    return (false, $"DHR responded with HTTP {(int)resp.StatusCode} ({resp.ReasonPhrase}).");
                }

                using var ping = await client.GetAsync("health", ct);
                return (true, $"DHR HTTPS endpoint reachable (HTTP {(int)ping.StatusCode}).");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DHR connectivity test failed for {Url}", apiBaseUrl);
            var detail = ex.InnerException is not null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
            return (false, detail);
        }
    }

    private async Task<System.Security.Cryptography.X509Certificates.X509Certificate2> RequireClientCertificateAsync(CancellationToken ct)
    {
        return await keyManager.LoadActiveClientCertificateAsync(ct)
            ?? throw new UnauthorizedAccessException("Connector client certificate is not installed. Activate the connector first.");
    }

    private HttpClient CreateHttpClient(string apiBaseUrl, System.Security.Cryptography.X509Certificates.X509Certificate2? clientCertificate)
    {
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Invalid DHR API base URL.");

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            if (!(options.Value.AllowLoopbackHttpForDevelopment && uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && uri.IsLoopback))
                throw new InvalidOperationException("Refusing insecure non-HTTPS connection to DHR.");
        }

        var handler = new HttpClientHandler
        {
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            CheckCertificateRevocationList = false
            // ServerCertificateCustomValidationCallback is intentionally NEVER bypassed.
        };

        if (clientCertificate is not null)
        {
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ClientCertificates.Add(clientCertificate);
        }

        return new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(uri.ToString().TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestVersion = System.Net.HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogWarning("{Operation} failed with HTTP {Status}: {Body}", operation, response.StatusCode, body);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException($"{operation} rejected by DHR (HTTP {(int)response.StatusCode}): {body}");

        throw new HttpRequestException($"{operation} failed (HTTP {(int)response.StatusCode}): {body}", null, response.StatusCode);
    }
}
