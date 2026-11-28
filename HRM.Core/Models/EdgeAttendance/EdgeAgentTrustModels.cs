namespace HRM.Models.EdgeAttendance;

public sealed class EdgeAttendanceAgent
{
    public int EdgeAttendanceAgentId { get; set; }
    public string AgentIdentifier { get; set; } = "";
    public int OrganisationId { get; set; }
    public string CertificateThumbprint { get; set; } = "";
    public string CertificateIssuer { get; set; } = "";
    public DateTime CertificateNotAfterUtc { get; set; }
    public bool IsActive { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public string? LastHealthStatus { get; set; }
    public string? SiteName { get; set; }
    public string? ConnectorVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public int ConfigurationVersion { get; set; } = 1;
    public DateTime? LastSyncAtUtc { get; set; }
    public int PendingEventCount { get; set; }
    public string? PublicKeyPem { get; set; }
}

public sealed class EdgeAttendanceTrustedIssuer
{
    public int EdgeAttendanceTrustedIssuerId { get; set; }
    public string Subject { get; set; } = "";
    public string Thumbprint { get; set; } = "";
    public byte[]? CertificateRawData { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class EdgeAttendanceAgentDevice
{
    public int EdgeAttendanceAgentDeviceId { get; set; }
    public int EdgeAttendanceAgentId { get; set; }
    public int AttendanceDeviceId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int PollingIntervalSeconds { get; set; } = 30;
}

public sealed class EdgeAttendanceEventReceipt
{
    public long EdgeAttendanceEventReceiptId { get; set; }
    public int EdgeAttendanceAgentId { get; set; }
    public int AttendanceDeviceId { get; set; }
    public string DeviceEventId { get; set; } = "";
    public string EventHash { get; set; } = "";
    public string LocalUserId { get; set; } = "";
    public DateTime DeviceTimestampUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public string ProcessingStatus { get; set; } = "Received";
    public int? AttendanceLogId { get; set; }
}

public sealed class EdgeAttendanceIngestionFailure
{
    public long EdgeAttendanceIngestionFailureId { get; set; }
    public int? EdgeAttendanceAgentId { get; set; }
    public int? AttendanceDeviceId { get; set; }
    public string FailureCode { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
}

public sealed class EdgeAttendanceActivationCode
{
    public int EdgeAttendanceActivationCodeId { get; set; }
    public string CodeHash { get; set; } = "";
    public string CodePrefix { get; set; } = "";
    public int OrganisationId { get; set; }
    public string? SiteName { get; set; }
    public string? SuggestedConnectorName { get; set; }
    public string? DeviceIdsJson { get; set; }
    public int? CreatedByIndividualId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int? UsedByEdgeAttendanceAgentId { get; set; }
    public bool IsRevoked { get; set; }
}

public sealed class EdgeAttendanceConnectorCommand
{
    public long EdgeAttendanceConnectorCommandId { get; set; }
    public int EdgeAttendanceAgentId { get; set; }
    public string CommandType { get; set; } = "";
    public string? PayloadJson { get; set; }
    public string Status { get; set; } = "Pending";
    public int? CreatedByIndividualId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
}
