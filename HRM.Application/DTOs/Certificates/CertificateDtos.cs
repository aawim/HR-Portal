using System;
using System.Collections.Generic;

namespace HRM.DTOs.Certificates;

public sealed class CertificateRequestInput
{
    public int IndividualId { get; set; }
    public string CertificateType { get; set; } = "StandardService"; // "StandardService" or "DetailedExperience"
    public string Purpose { get; set; } = string.Empty;
    public string? PurposeDetails { get; set; }
    public string RequestScope { get; set; } = "FullCareer"; // "FullCareer", "SpecificPeriod", "SpecificRoles", "SpecificProjects"
    public DateTime? SpecificPeriodStart { get; set; }
    public DateTime? SpecificPeriodEnd { get; set; }
    public List<int>? SelectedPositionIds { get; set; }
    public List<int>? SelectedProjectIds { get; set; }
    public bool IncludeResponsibilities { get; set; } = true;
    public bool IncludeProjects { get; set; }
    public bool IncludeSalary { get; set; }
    public string? AddressedTo { get; set; } = "To Whom It May Concern";
    public string? EmployeeComments { get; set; }
}

public sealed class CertificateReviewActionInput
{
    public int CertificateRequestId { get; set; }
    public string Action { get; set; } = string.Empty; // "StartReview", "Return", "Approve", "Issue", "Reject"
    public string? Comments { get; set; }
    public string? Reason { get; set; }
    public string? CorrectedAddressedTo { get; set; }
    public string? CorrectedSignatoryName { get; set; }
    public string? CorrectedSignatoryTitle { get; set; }
    public bool? IncludeResponsibilities { get; set; }
    public bool? IncludeProjects { get; set; }
    public bool? IncludeSalary { get; set; }
    public DateTime? SpecificPeriodStart { get; set; }
    public DateTime? SpecificPeriodEnd { get; set; }
}

public sealed class CertificateRevokeInput
{
    public int IssuedCertificateId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class CertificateReissueInput
{
    public int IssuedCertificateId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CorrectedAddressedTo { get; set; }
    public string? CorrectedSignatoryName { get; set; }
    public string? CorrectedSignatoryTitle { get; set; }
}

public sealed class CertificateRequestSummaryDto
{
    public int CertificateRequestId { get; set; }
    public int RequestId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public int IndividualId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string CurrentDesignation { get; set; } = string.Empty;
    public string CertificateType { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string RequestScope { get; set; } = "FullCareer";
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? AddressedTo { get; set; }
    public int? IssuedCertificateId { get; set; }
    public string? CertificateNumber { get; set; }
    public string? VerificationToken { get; set; }
    public string? ReturnReason { get; set; }
    public string? RejectionReason { get; set; }
}

public sealed class CertificateRequestDetailDto
{
    public CertificateRequestSummaryDto Summary { get; set; } = new();
    public CertificateVerifiedEmploymentHistoryDto EmploymentHistory { get; set; } = new();
    public CertificateSnapshotDto? CurrentDraftSnapshot { get; set; }
    public IssuedCertificateDetailDto? IssuedCertificate { get; set; }
    public List<CertificateAuditLogDto> AuditTrail { get; set; } = [];
}

public sealed class CertificateVerifiedEmploymentHistoryDto
{
    public int IndividualId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; }
    public DateTime? TerminatedDate { get; set; }
    public bool IsCurrentlyEmployed { get; set; }
    public int TotalServiceYears { get; set; }
    public int TotalServiceMonths { get; set; }
    public int TotalServiceDays { get; set; }
    public string FormattedServiceDuration { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string CurrentJobTitle { get; set; } = string.Empty;
    public decimal? CurrentBasicSalary { get; set; }
    public List<CertificatePositionItemDto> Positions { get; set; } = [];
    public List<CertificateProjectItemDto> Projects { get; set; } = [];
}

public sealed class CertificatePositionItemDto
{
    public int JobPositionId { get; set; }
    public int PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsCurrent { get; set; }
    public List<string> OfficialResponsibilities { get; set; } = [];
}

public sealed class CertificateProjectItemDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectReference { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public sealed class IssuedCertificateDetailDto
{
    public int IssuedCertificateId { get; set; }
    public int CertificateRequestId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    public int IndividualId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string CertificateType { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryTitle { get; set; } = string.Empty;
    public string IssuingOfficerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // "Active", "Revoked", "Superseded"
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public bool HasPdf { get; set; }
    public CertificateSnapshotDto Snapshot { get; set; } = new();
}

public sealed class CertificateSnapshotDto
{
    public string CertificateNumber { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = string.Empty;
    public string CertificateType { get; set; } = string.Empty;
    public string CertificateTitle { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string AddressedTo { get; set; } = "TO WHOM IT MAY CONCERN";

    // Organisation & Letterhead
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string OrganisationRegistrationNumber { get; set; } = string.Empty;
    public string HeaderNotes { get; set; } = string.Empty;

    // Employee identity
    public int IndividualId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime EmploymentStartDate { get; set; }
    public DateTime? EmploymentEndDate { get; set; }
    public bool IsCurrentlyEmployed { get; set; }
    public string FormattedDuration { get; set; } = string.Empty;
    public string PrimaryDepartment { get; set; } = string.Empty;
    public string PrimaryDesignation { get; set; } = string.Empty;
    public decimal? BasicSalary { get; set; }

    // Content paragraphs
    public string IntroductoryText { get; set; } = string.Empty;
    public string ServiceStatement { get; set; } = string.Empty;
    public string ConcludingText { get; set; } = string.Empty;

    // Verified details lists
    public List<CertificatePositionSnapshotItemDto> Positions { get; set; } = [];
    public List<CertificateProjectSnapshotItemDto> Projects { get; set; } = [];

    // Signatory
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryTitle { get; set; } = string.Empty;
    public string IssuingOfficerName { get; set; } = string.Empty;
    public string IssuingOfficerTitle { get; set; } = string.Empty;

    // Integrity
    public string Sha256Hash { get; set; } = string.Empty;
}

public sealed class CertificatePositionSnapshotItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string PeriodText { get; set; } = string.Empty;
    public List<string> Responsibilities { get; set; } = [];
}

public sealed class CertificateProjectSnapshotItemDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PeriodText { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public sealed class CertificateVerificationResultDto
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = "NotFound"; // "Valid", "Revoked", "Superseded", "NotFound"
    public string CertificateNumber { get; set; } = string.Empty;
    public string CertificateType { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateTime? EmploymentStartDate { get; set; }
    public DateTime? EmploymentEndDate { get; set; }
    public string FormattedDuration { get; set; } = string.Empty;
    public string PrimaryJobTitle { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryTitle { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? SupersededByCertificateNumber { get; set; }
    public DateTime VerificationTimestamp { get; set; } = DateTime.UtcNow;
}

public sealed class CertificateAuditLogDto
{
    public int CertificateAuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedByUserName { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
