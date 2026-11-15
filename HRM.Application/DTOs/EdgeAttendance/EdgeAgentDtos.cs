namespace HRM.DTOs.EdgeAttendance;
public sealed record EdgeAgentSummaryDto(int Id,string AgentIdentifier,int OrganisationId,string Thumbprint,string Issuer,DateTime ExpiresUtc,bool IsActive,DateTime? RevokedUtc,DateTime? LastSeenUtc,string? HealthStatus,int DeviceCount,int FailureCount);
public sealed class EdgeAgentRegistrationInput { public string AgentIdentifier { get; set; }=""; public int OrganisationId { get; set; } public string CertificateThumbprint { get; set; }=""; public string CertificateIssuer { get; set; }=""; public DateTime CertificateNotAfterUtc { get; set; } public List<int> AttendanceDeviceIds { get; set; }=[]; }
public sealed record EdgeAttendanceDeviceDto(int Id,string IpAddress,int Port,string? Location,int? OrganisationId,string? AdapterType,int? AgentId,bool IsEdgeEnabled);
public sealed record EdgeAttendanceIngestionFailureDto(long Id,int? AgentId,int? DeviceId,string Code,string Detail,DateTime OccurredAtUtc);
public sealed record EdgeTrustedIssuerDto(int Id,string Subject,string Thumbprint,bool IsActive,DateTime CreatedAtUtc);
public sealed class EdgeTrustedIssuerInput { public string CertificateBase64 { get; set; } = ""; }
public sealed class EdgeAttendanceDeviceConfigurationInput { public int AttendanceDeviceId { get; set; } public int OrganisationId { get; set; } public int? EdgeAttendanceAgentId { get; set; } public string? AdapterType { get; set; } public bool IsEdgeEnabled { get; set; } = true; }
