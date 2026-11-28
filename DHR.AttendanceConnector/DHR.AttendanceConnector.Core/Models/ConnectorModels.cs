namespace DHR.AttendanceConnector.Core.Models;

public enum ConnectorStatusState
{
    RegistrationRequired,
    Connected,
    Offline,
    AuthenticationError,
    SyncDelayed
}

public enum OutboxEventState
{
    Collected,
    PendingUpload,
    Uploading,
    Acknowledged,
    Rejected,
    Quarantined
}

public sealed record ConnectorProfile(
    int ConnectorId,
    string AgentIdentifier,
    int OrganisationId,
    string OrganisationName,
    string? SiteName,
    string ApiBaseUrl,
    string CertificateThumbprint,
    DateTime CertificateNotAfterUtc,
    int ConfigurationVersion,
    int DefaultPollingIntervalSeconds,
    int SyncBatchSize,
    int HeartbeatIntervalSeconds,
    int RetryBaseDelaySeconds,
    int RetryMaxDelaySeconds,
    DateTime ActivatedAtUtc,
    DateTime? LastConfigSyncAtUtc);

public sealed record DeviceConfiguration(
    int DeviceId,
    string Name,
    string IpAddress,
    int Port,
    string ProtocolCode,
    int PollingIntervalSeconds,
    bool Enabled,
    string? LastConnectionStatus = null,
    DateTime? LastSuccessfulPollUtc = null,
    DateTime? LastEventTimestampUtc = null,
    string? LastError = null);

public sealed record DeviceConnectionResult(
    bool IsReachable,
    long LatencyMs,
    string? ErrorMessage = null,
    string? FirmwareVersion = null,
    string? SerialNumber = null);

public sealed record DeviceInformation(
    string Vendor,
    string Model,
    string SerialNumber,
    string FirmwareVersion,
    DateTime? DeviceClockUtc = null,
    int? StoredLogCount = null);

public sealed record DeviceSyncCheckpoint(
    int DeviceId,
    string? LastEventId,
    DateTime? LastEventTimestampUtc,
    long TotalEventsCollected,
    DateTime UpdatedAtUtc);

public sealed record RawAttendanceEvent(
    long LocalId,
    string ConnectorId,
    int DeviceId,
    string DeviceUserId,
    DateTime DeviceTimestampUtc,
    string DeviceEventId,
    string? PunchStatus,
    string SourceProtocol,
    string? RawMetadata,
    DateTime IngestedAtUtc,
    string DeduplicationKey,
    OutboxEventState State = OutboxEventState.Collected,
    int RetryCount = 0,
    DateTime? NextRetryAtUtc = null,
    string? LastError = null)
{
    public DateTime DeviceTimestampLocal => DeviceTimestampUtc.ToLocalTime();
}

public sealed record SyncAcknowledgement(
    long Id,
    string BatchId,
    int AcceptedCount,
    int DuplicateCount,
    int RejectedCount,
    DateTime AcknowledgedAtUtc,
    string? ServerMessage = null);

public sealed record AgentCommandRecord(
    long CommandId,
    string CommandType,
    string? PayloadJson,
    string Status,
    DateTime ReceivedAtUtc,
    DateTime? ExecutedAtUtc = null,
    string? ResultJson = null,
    string? ErrorMessage = null);

public sealed record ConnectorHealthLogEntry(
    long Id,
    DateTime TimestampUtc,
    string Level,
    string Component,
    string Message,
    string? Detail = null)
{
    public DateTime TimestampLocal => TimestampUtc.ToLocalTime();
}

public sealed record ConfigurationVersionRecord(
    int VersionNumber,
    string ConfigHash,
    DateTime AppliedAtUtc,
    int DeviceCount);

public sealed record SyncMetricsSnapshot(
    int PendingCount,
    int UploadingCount,
    int AcknowledgedCount,
    int RejectedCount,
    int QuarantinedCount,
    DateTime? LastSuccessfulSyncUtc);

public sealed record ConnectorDashboardSnapshot(
    ConnectorStatusState Status,
    string StatusMessage,
    ConnectorProfile? Profile,
    IReadOnlyList<DeviceConfiguration> Devices,
    SyncMetricsSnapshot SyncMetrics,
    bool LocalStorageHealthy,
    bool DhrReachable,
    string ConnectorVersion,
    DateTime? LastHeartbeatUtc,
    IReadOnlyList<ConnectorHealthLogEntry> RecentLogs);
