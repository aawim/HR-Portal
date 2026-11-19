using DHR.AttendanceConnector.Core.Models;

namespace DHR.AttendanceConnector.Core.Interfaces;

public interface IConnectorRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<bool> CheckHealthAsync(CancellationToken ct = default);

    // ConnectorConfiguration
    Task<ConnectorProfile?> GetProfileAsync(CancellationToken ct = default);
    Task SaveProfileAsync(ConnectorProfile profile, CancellationToken ct = default);
    Task ClearProfileAsync(CancellationToken ct = default);

    // RegisteredDevices
    Task<IReadOnlyList<DeviceConfiguration>> GetDevicesAsync(CancellationToken ct = default);
    Task ReplaceDevicesAsync(IReadOnlyList<DeviceConfiguration> devices, int configVersion, string configHash, CancellationToken ct = default);
    Task UpdateDeviceHealthAsync(int deviceId, string status, DateTime? lastSuccessfulPollUtc, DateTime? lastEventTimestampUtc, string? lastError, CancellationToken ct = default);

    // DeviceSyncCheckpoints
    Task<DeviceSyncCheckpoint?> GetCheckpointAsync(int deviceId, CancellationToken ct = default);
    Task SaveCheckpointAsync(DeviceSyncCheckpoint checkpoint, CancellationToken ct = default);

    // RawAttendanceEvents & OutboundSyncQueue (Transactional Outbox)
    Task<int> PersistCollectedEventsAsync(IReadOnlyList<RawAttendanceEvent> events, CancellationToken ct = default);
    Task<IReadOnlyList<RawAttendanceEvent>> ClaimPendingBatchAsync(int maxBatchSize, string batchId, CancellationToken ct = default);
    Task RecoverStaleUploadingEventsAsync(TimeSpan staleThreshold, CancellationToken ct = default);
    Task ApplyBatchAcknowledgementAsync(
        string batchId,
        IReadOnlyList<long> localIds,
        int accepted,
        int duplicate,
        int rejected,
        string? serverMessage,
        CancellationToken ct = default);
    Task RecordBatchFailureAsync(
        string batchId,
        IReadOnlyList<long> localIds,
        string errorMessage,
        DateTime nextRetryAtUtc,
        int maxRetriesBeforeQuarantine = 20,
        CancellationToken ct = default);
    Task<int> RetryPendingAndQuarantinedAsync(CancellationToken ct = default);
    Task<SyncMetricsSnapshot> GetSyncMetricsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RawAttendanceEvent>> GetRecentEventsAsync(int limit = 100, CancellationToken ct = default);

    // AgentCommands
    Task SaveReceivedCommandAsync(AgentCommandRecord command, CancellationToken ct = default);
    Task UpdateCommandResultAsync(long commandId, string status, string? resultJson, string? errorMessage, CancellationToken ct = default);
    Task<IReadOnlyList<AgentCommandRecord>> GetRecentCommandsAsync(int limit = 50, CancellationToken ct = default);

    // ConnectorHealthLogs
    Task WriteHealthLogAsync(string level, string component, string message, string? detail = null, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectorHealthLogEntry>> GetHealthLogsAsync(int limit = 200, CancellationToken ct = default);

    // ConfigurationVersions
    Task<IReadOnlyList<ConfigurationVersionRecord>> GetConfigurationHistoryAsync(int limit = 20, CancellationToken ct = default);
}

public interface ISecretStore
{
    Task SaveSecretAsync(string key, byte[] plaintext, CancellationToken ct = default);
    Task<byte[]?> LoadSecretAsync(string key, CancellationToken ct = default);
    Task DeleteSecretAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}

public interface IDeviceIdentityKeyManager
{
    Task<(string PublicKeyPem, string CsrPem, string ProofSignatureBase64, DateTime ProofTimestampUtc)> GenerateEnrollmentKeyAndProofAsync(
        string activationCode,
        string connectorName,
        CancellationToken ct = default);

    Task<(string NewPublicKeyPem, string NewCsrPem, string ProofSignatureBase64, DateTime ProofTimestampUtc)> GenerateRenewalKeyAndProofAsync(
        string agentId,
        CancellationToken ct = default);

    Task CommitIssuedCertificateAsync(string clientCertPem, string caCertPem, bool isRenewal, CancellationToken ct = default);
    Task<System.Security.Cryptography.X509Certificates.X509Certificate2?> LoadActiveClientCertificateAsync(CancellationToken ct = default);
    Task ClearCredentialsAsync(CancellationToken ct = default);
}
