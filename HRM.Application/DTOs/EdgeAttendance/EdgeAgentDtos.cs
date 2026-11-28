namespace HRM.DTOs.EdgeAttendance;

public sealed record EdgeAgentSummaryDto(
    int Id,
    string AgentIdentifier,
    int OrganisationId,
    string Thumbprint,
    string Issuer,
    DateTime ExpiresUtc,
    bool IsActive,
    DateTime? RevokedUtc,
    DateTime? LastSeenUtc,
    string? HealthStatus,
    int DeviceCount,
    int FailureCount,
    string? SiteName = null,
    string? ConnectorVersion = null,
    string? OperatingSystem = null,
    int ConfigurationVersion = 1,
    DateTime? LastSyncUtc = null,
    int PendingEventCount = 0);

public sealed class EdgeAgentRegistrationInput
{
    public string AgentIdentifier { get; set; } = "";
    public int OrganisationId { get; set; }
    public string CertificateThumbprint { get; set; } = "";
    public string CertificateIssuer { get; set; } = "";
    public DateTime CertificateNotAfterUtc { get; set; }
    public List<int> AttendanceDeviceIds { get; set; } = [];
}

public sealed record EdgeAttendanceDeviceDto(
    int Id,
    string IpAddress,
    int Port,
    string? Location,
    int? OrganisationId,
    string? AdapterType,
    int? AgentId,
    bool IsEdgeEnabled,
    int PollingIntervalSeconds = 30);

public sealed record EdgeAttendanceIngestionFailureDto(
    long Id,
    int? AgentId,
    int? DeviceId,
    string Code,
    string Detail,
    DateTime OccurredAtUtc);

public sealed record EdgeTrustedIssuerDto(
    int Id,
    string Subject,
    string Thumbprint,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed class EdgeTrustedIssuerInput
{
    public string CertificateBase64 { get; set; } = "";
}

public sealed class EdgeAttendanceDeviceConfigurationInput
{
    public int AttendanceDeviceId { get; set; }
    public int OrganisationId { get; set; }
    public int? EdgeAttendanceAgentId { get; set; }
    public string? AdapterType { get; set; }
    public bool IsEdgeEnabled { get; set; } = true;
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public string? Location { get; set; }
    public int PollingIntervalSeconds { get; set; } = 30;
}

public sealed class EdgeReconciliationFilterInput
{
    public int? OrganisationId { get; set; }
}

public sealed record EdgeReconciliationResultDto(int Evaluated, int Resolved, int RemainingPending);

public sealed record EdgePendingReceiptDto(
    long ReceiptId,
    int AgentId,
    string AgentIdentifier,
    int DeviceId,
    string DeviceEventId,
    string LocalUserId,
    DateTime DeviceTimestampUtc,
    DateTime ReceivedAtUtc,
    string ProcessingStatus,
    int OrganisationId);

public sealed class ResolvePendingReceiptInput
{
    public long ReceiptId { get; set; }
    public int IndividualId { get; set; }
    public bool AssignEnrollmentNumberToStaff { get; set; } = true;
}

public sealed class EdgeStaffSearchInput
{
    public int OrganisationId { get; set; }
    public string? Query { get; set; }
}

public sealed record EdgeStaffOptionDto(
    int IndividualId,
    string FullName,
    string? SapNumber,
    string? CurrentEnrollmentNumber);

// --- Enterprise Attendance Connector Enrollment, Commands & Alerts DTOs ---

public sealed class CreateActivationCodeInput
{
    public int OrganisationId { get; set; }
    public string? SiteName { get; set; }
    public string? SuggestedConnectorName { get; set; }
    public int ExpiresInMinutes { get; set; } = 60;
    public List<int> AttendanceDeviceIds { get; set; } = [];
}

public sealed record CreatedActivationCodeDto(
    int ActivationCodeId,
    string PlaintextCode,
    string CodePrefix,
    int OrganisationId,
    string? SiteName,
    string? SuggestedConnectorName,
    DateTime ExpiresAtUtc);

public sealed record EdgeActivationCodeDto(
    int Id,
    string CodePrefix,
    int OrganisationId,
    string? SiteName,
    string? SuggestedConnectorName,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? UsedAtUtc,
    int? UsedByAgentId,
    bool IsRevoked);

public sealed class QueueConnectorCommandInput
{
    public int EdgeAttendanceAgentId { get; set; }
    public string CommandType { get; set; } = "";
    public string? PayloadJson { get; set; }
}

public sealed record EdgeConnectorCommandDto(
    long Id,
    int AgentId,
    string AgentIdentifier,
    string CommandType,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? DispatchedAtUtc,
    DateTime? CompletedAtUtc,
    string? ResultJson,
    string? ErrorMessage);

public sealed record EdgeConnectorAlertDto(
    string Severity,
    string AlertCode,
    int? AgentId,
    string? AgentIdentifier,
    int? OrganisationId,
    string Title,
    string Message,
    DateTime DetectedAtUtc);
