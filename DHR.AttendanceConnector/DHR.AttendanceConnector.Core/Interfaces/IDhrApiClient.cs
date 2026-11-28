using DHR.AttendanceConnector.Core.Models;

namespace DHR.AttendanceConnector.Core.Interfaces;

public sealed record EnrollConnectorPayload(
    string ActivationCode,
    string ConnectorName,
    string OperatingSystem,
    string ConnectorVersion,
    string PublicKeyPem,
    string CsrPem,
    string ProofSignatureBase64,
    DateTime ProofTimestampUtc);

public sealed record RemoteConnectorDeviceDto(
    int DeviceId,
    string Name,
    string IpAddress,
    int Port,
    string ProtocolCode,
    int PollingIntervalSeconds,
    bool Enabled);

public sealed record RemoteConnectorConfiguration(
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
    IReadOnlyList<RemoteConnectorDeviceDto> Devices);

public sealed record RemoteEnrollmentResult(
    int ConnectorId,
    string AgentIdentifier,
    int OrganisationId,
    string OrganisationName,
    string? SiteName,
    string ClientCertificatePem,
    string CaCertificatePem,
    string CertificateThumbprint,
    DateTime CertificateNotAfterUtc,
    RemoteConnectorConfiguration Configuration);

public sealed record RemoteBatchEventItem(
    int DeviceId,
    string LocalUserId,
    DateTime DeviceTimestamp,
    string DeviceEventId,
    string? RawMetadata);

public sealed record RemoteBatchResult(
    int Accepted,
    int Duplicate,
    int Rejected);

public sealed record RemoteHeartbeatPayload(
    string AgentId,
    string HealthStatus,
    string? ConnectorVersion,
    string? OperatingSystem,
    int PendingEventCount,
    int ConfigurationVersion,
    DateTime? LastSyncAtUtc);

public sealed record RemoteCommandItem(
    long CommandId,
    string CommandType,
    string? PayloadJson,
    DateTime CreatedAtUtc);

public sealed record RemoteRenewalResult(
    string ClientCertificatePem,
    string CaCertificatePem,
    string CertificateThumbprint,
    DateTime CertificateNotAfterUtc);

public interface IDhrApiClient
{
    Task<RemoteEnrollmentResult> EnrollAsync(string apiBaseUrl, EnrollConnectorPayload payload, CancellationToken ct = default);
    Task<RemoteConnectorConfiguration> GetConfigurationAsync(string apiBaseUrl, string agentId, CancellationToken ct = default);
    Task<RemoteBatchResult> UploadBatchAsync(string apiBaseUrl, string agentId, IReadOnlyList<RemoteBatchEventItem> events, CancellationToken ct = default);
    Task SendHeartbeatAsync(string apiBaseUrl, RemoteHeartbeatPayload payload, CancellationToken ct = default);
    Task<IReadOnlyList<RemoteCommandItem>> PollCommandsAsync(string apiBaseUrl, string agentId, CancellationToken ct = default);
    Task ReportCommandResultAsync(string apiBaseUrl, long commandId, string agentId, bool succeeded, string? resultJson, string? errorMessage, CancellationToken ct = default);
    Task<RemoteRenewalResult> RenewCredentialsAsync(string apiBaseUrl, string agentId, string newPublicKeyPem, string newCsrPem, string proofSignatureBase64, DateTime proofTimestampUtc, CancellationToken ct = default);
    Task<(bool Reachable, string Message)> TestConnectionAsync(string apiBaseUrl, string? agentId, CancellationToken ct = default);
}
