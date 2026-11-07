using System;
using System.Collections.Generic;
using HRM.Models.Archives;

namespace HRM.Models;

/// <summary>
/// Certificate request header linking to standard workflow request.
/// Mapped to dbo.EC_CertificateRequests.
/// </summary>
public sealed class CertificateRequest
{
    public int CertificateRequestId { get; set; }
    public int RequestId { get; set; }
    public int IndividualId { get; set; }
    public int OrganisationId { get; set; }
    public string CertificateType { get; set; } = "StandardService"; // 'StandardService' or 'DetailedExperience'
    public string Purpose { get; set; } = string.Empty;
    public string? PurposeDetails { get; set; }
    public string RequestScope { get; set; } = "FullCareer"; // 'FullCareer', 'SpecificPeriod', 'SpecificRoles', 'SpecificProjects'
    public DateTime? SpecificPeriodStart { get; set; }
    public DateTime? SpecificPeriodEnd { get; set; }
    public string? SelectedPositionIdsJson { get; set; }
    public string? SelectedProjectIdsJson { get; set; }
    public bool IncludeResponsibilities { get; set; } = true;
    public bool IncludeProjects { get; set; }
    public bool IncludeSalary { get; set; }
    public string? AddressedTo { get; set; } = "To Whom It May Concern";
    public string? EmployeeComments { get; set; }
    public string Status { get; set; } = "Submitted"; // 'Submitted', 'UnderReview', 'Approved', 'Issued', 'ReturnedForClarification', 'Rejected', 'Cancelled'
    public string? ReviewerComments { get; set; }
    public string? ReturnedReason { get; set; }
    public string? RejectionReason { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }

    public Request Request { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public ICollection<IssuedCertificate> IssuedCertificates { get; set; } = new List<IssuedCertificate>();
    public ICollection<CertificateAuditLog> AuditLogs { get; set; } = new List<CertificateAuditLog>();
}

/// <summary>
/// Issued official certificate with immutable snapshot, digital verification token and SHA-256 hash.
/// Mapped to dbo.EC_IssuedCertificates.
/// </summary>
public sealed class IssuedCertificate
{
    public int IssuedCertificateId { get; set; }
    public int CertificateRequestId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    public int IndividualId { get; set; }
    public int OrganisationId { get; set; }
    public string CertificateType { get; set; } = "StandardService";
    public DateTime IssueDate { get; set; }
    public int IssuingOfficerUserId { get; set; }
    public string IssuingOfficerName { get; set; } = string.Empty;
    public string IssuingOfficerTitle { get; set; } = string.Empty;
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryTitle { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public byte[]? PdfContent { get; set; }
    public DateTime? PdfGeneratedAt { get; set; }
    public string Status { get; set; } = "Active"; // 'Active', 'Revoked', 'Superseded'
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int? RevokedByUserId { get; set; }
    public int? SupersededByCertificateId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CertificateRequest CertificateRequest { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
}

/// <summary>
/// Configurable organization-specific certificate template.
/// Mapped to dbo.EC_CertificateTemplates.
/// </summary>
public sealed class CertificateTemplate
{
    public int CertificateTemplateId { get; set; }
    public int OrganisationId { get; set; }
    public string CertificateType { get; set; } = "StandardService";
    public string Title { get; set; } = string.Empty;
    public string? HeaderLetterheadHtml { get; set; }
    public string? BodyIntroduction { get; set; }
    public string? FooterNotes { get; set; }
    public string? DefaultSignatoryName { get; set; }
    public string? DefaultSignatoryTitle { get; set; }
    public bool ShowLetterhead { get; set; } = true;
    public bool ShowQrCode { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organisation Organisation { get; set; } = null!;
}

/// <summary>
/// Immutable audit log for certificate lifecycle events.
/// Mapped to dbo.EC_CertificateAuditLogs.
/// </summary>
public sealed class CertificateAuditLog
{
    public int CertificateAuditLogId { get; set; }
    public int? CertificateRequestId { get; set; }
    public int? IssuedCertificateId { get; set; }
    public int OrganisationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public CertificateRequest? CertificateRequest { get; set; }
}
