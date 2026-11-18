namespace HRM.DTOs.EdgeAttendance;
public sealed record EdgeAgentSummaryDto(int Id,string AgentIdentifier,int OrganisationId,string Thumbprint,string Issuer,DateTime ExpiresUtc,bool IsActive,DateTime? RevokedUtc,DateTime? LastSeenUtc,string? HealthStatus,int DeviceCount,int FailureCount);
public sealed class EdgeAgentRegistrationInput { public string AgentIdentifier { get; set; }=""; public int OrganisationId { get; set; } public string CertificateThumbprint { get; set; }=""; public string CertificateIssuer { get; set; }=""; public DateTime CertificateNotAfterUtc { get; set; } public List<int> AttendanceDeviceIds { get; set; }=[]; }
public sealed record EdgeAttendanceDeviceDto(int Id,string IpAddress,int Port,string? Location,int? OrganisationId,string? AdapterType,int? AgentId,bool IsEdgeEnabled);
public sealed record EdgeAttendanceIngestionFailureDto(long Id,int? AgentId,int? DeviceId,string Code,string Detail,DateTime OccurredAtUtc);
public sealed record EdgeTrustedIssuerDto(int Id,string Subject,string Thumbprint,bool IsActive,DateTime CreatedAtUtc);
public sealed class EdgeTrustedIssuerInput { public string CertificateBase64 { get; set; } = ""; }
public sealed class EdgeAttendanceDeviceConfigurationInput { public int AttendanceDeviceId { get; set; } public int OrganisationId { get; set; } public int? EdgeAttendanceAgentId { get; set; } public string? AdapterType { get; set; } public bool IsEdgeEnabled { get; set; } = true; public string? IpAddress { get; set; } public int? Port { get; set; } public string? Location { get; set; } }
public sealed class EdgeReconciliationFilterInput { public int? OrganisationId { get; set; } }
public sealed record EdgeReconciliationResultDto(int Evaluated, int Resolved, int RemainingPending);
public sealed record EdgePendingReceiptDto(long ReceiptId, int AgentId, string AgentIdentifier, int DeviceId, string DeviceEventId, string LocalUserId, DateTime DeviceTimestampUtc, DateTime ReceivedAtUtc, string ProcessingStatus, int OrganisationId);
public sealed class ResolvePendingReceiptInput { public long ReceiptId { get; set; } public int IndividualId { get; set; } public bool AssignEnrollmentNumberToStaff { get; set; } = true; }
public sealed class EdgeStaffSearchInput { public int OrganisationId { get; set; } public string? Query { get; set; } }
public sealed record EdgeStaffOptionDto(int IndividualId, string FullName, string? SapNumber, string? CurrentEnrollmentNumber);
