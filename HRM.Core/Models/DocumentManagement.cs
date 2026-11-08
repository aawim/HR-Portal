using System;
using System.Collections.Generic;
using HRM.Models.Archives;

namespace HRM.Models;

/// <summary>
/// Configurable Document Category for DHR-DMS.
/// Mapped to dbo.DMS_Categories.
/// </summary>
public sealed class DmsCategory
{
    public int CategoryId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TargetDomain { get; set; } = "General"; // 'Staff', 'Organisation', 'Workflow', 'Asset', 'Project', 'Evaluation', 'Transfer', 'Legal', 'General'
    public int DisplayOrder { get; set; }
    public bool IsSystemCategory { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Organisation? Organisation { get; set; }
    public ICollection<DmsDocumentType> DocumentTypes { get; set; } = new List<DmsDocumentType>();
    public ICollection<DmsDocument> Documents { get; set; } = new List<DmsDocument>();
}

/// <summary>
/// Configurable Document Type Policy for DHR-DMS.
/// Mapped to dbo.DMS_DocumentTypes.
/// </summary>
public sealed class DmsDocumentType
{
    public int DmsDocumentTypeId { get; set; }
    public int CategoryId { get; set; }
    public int? LegacyDocumentTypeId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameDhivehi { get; set; }
    public string? Description { get; set; }
    public string DefaultClassification { get; set; } = "Internal"; // 'Public', 'Internal', 'Confidential', 'Restricted', 'Secret'
    public string DefaultAccessScope { get; set; } = "EmployeeAndHr"; // 'PublicOrg', 'DepartmentAndHr', 'EmployeeAndHr', 'SupervisorAndHr', 'HrOnly', 'ExecutiveOnly', 'RestrictedCustom'
    public string PermittedExtensions { get; set; } = ".pdf,.png,.jpg,.jpeg,.docx,.xlsx";
    public int MaxFileSizeMb { get; set; } = 15;
    public string? RequiredMetadataJson { get; set; }
    public bool RequiresApproval { get; set; }
    public bool RequiresVerification { get; set; }
    public bool RequiresSignature { get; set; }
    public bool IsOfficialRecordWhenApproved { get; set; }
    public bool AllowEmployeeUpload { get; set; } = true;
    public bool AllowEmployeeRequest { get; set; }
    public int? RetentionPolicyId { get; set; }
    public bool IsSystemType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public DmsCategory Category { get; set; } = null!;
    public DocumentType? LegacyDocumentType { get; set; }
    public Organisation? Organisation { get; set; }
    public DmsRetentionPolicy? RetentionPolicy { get; set; }
    public ICollection<DmsDocument> Documents { get; set; } = new List<DmsDocument>();
}

/// <summary>
/// Retention Schedule & Archive Rule for Enterprise Records Governance.
/// Mapped to dbo.DMS_RetentionPolicies.
/// </summary>
public sealed class DmsRetentionPolicy
{
    public int RetentionPolicyId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RetentionMonths { get; set; } // Null = Permanent unless superseded
    public string TriggerEvent { get; set; } = "CreationDate"; // 'CreationDate', 'IssueDate', 'ExpiryDate', 'EmploymentEndDate', 'SupersededDate'
    public int? ArchiveAfterMonths { get; set; }
    public string DispositionAction { get; set; } = "PermanentKeep"; // 'PermanentKeep', 'ArchiveOnly', 'ReviewRequired', 'FormalDisposal'
    public bool RequiresApprovalToDispose { get; set; } = true;
    public string? PolicyReferenceNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Organisation? Organisation { get; set; }
}

/// <summary>
/// Private Immutable Binary Object Storage for Multi-Instance API Deployments.
/// Mapped to dbo.DMS_StoredObjects.
/// </summary>
public sealed class DmsStoredObject
{
    public long StoredObjectId { get; set; }
    public string StorageKey { get; set; } = string.Empty; // Generated internal GUID key
    public int OrganisationId { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public string FileExtension { get; set; } = ".pdf";
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = "SqlServerPrivateStore";
    public byte[] BinaryContent { get; set; } = Array.Empty<byte>();
    public string MalwareScanStatus { get; set; } = "Clean"; // 'Clean', 'Flagged', 'Quarantined'
    public string? MalwareScanDetails { get; set; }
    public DateTime ScannedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }

    public Organisation Organisation { get; set; } = null!;
}

/// <summary>
/// Central Enterprise Document & Official Record Header.
/// Mapped to dbo.DMS_Documents.
/// </summary>
public sealed class DmsDocument
{
    public long DocumentId { get; set; }
    public int? LegacyDocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int OrganisationId { get; set; }
    public int? DepartmentStructureId { get; set; }
    public int? SubjectIndividualId { get; set; }
    public int? SubjectJobId { get; set; }
    public int CategoryId { get; set; }
    public int DmsDocumentTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public string Classification { get; set; } = "Internal"; // 'Public', 'Internal', 'Confidential', 'Restricted', 'Secret'
    public string AccessScope { get; set; } = "EmployeeAndHr"; // 'PublicOrg', 'DepartmentAndHr', 'EmployeeAndHr', 'SupervisorAndHr', 'HrOnly', 'ExecutiveOnly', 'RestrictedCustom'
    public string Status { get; set; } = "Active"; // 'Draft', 'PendingVerification', 'PendingApproval', 'Approved', 'OfficialRecord', 'Published', 'Superseded', 'Revoked', 'Archived', 'Disposed', 'Deleted'
    public int CurrentVersionNumber { get; set; } = 1;
    public long? CurrentVersionId { get; set; }
    public bool IsOfficialRecord { get; set; }
    public bool IsImmutable { get; set; }
    public bool IsLockedForEdit { get; set; }
    public int? CheckedOutByUserId { get; set; }
    public string? CheckedOutByUserName { get; set; }
    public DateTime? CheckedOutAtUtc { get; set; }
    public string? CheckOutNote { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? EffectiveFromDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Tags { get; set; }
    public string? CustomMetadataJson { get; set; }
    public int? LinkedRequestId { get; set; }
    public bool IsVerified { get; set; }
    public int? VerifiedByUserId { get; set; }
    public string? VerifiedByUserName { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? VerificationNotes { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public long? SupersededByDocumentId { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int? RevokedByUserId { get; set; }
    public int? RetentionPolicyId { get; set; }
    public DateTime? RetentionDueDateUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public int? ArchivedByUserId { get; set; }
    public DateTime? DisposedAtUtc { get; set; }
    public int? DisposedByUserId { get; set; }
    public string? DisposalCertificateRef { get; set; }
    public bool HasActiveLegalHold { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Organisation Organisation { get; set; } = null!;
    public Individual? SubjectIndividual { get; set; }
    public DmsCategory Category { get; set; } = null!;
    public DmsDocumentType DocumentType { get; set; } = null!;
    public DmsRetentionPolicy? RetentionPolicy { get; set; }
    public Request? LinkedRequest { get; set; }

    public ICollection<DmsDocumentVersion> Versions { get; set; } = new List<DmsDocumentVersion>();
    public ICollection<DmsDocumentLink> Links { get; set; } = new List<DmsDocumentLink>();
    public ICollection<DmsDocumentPermission> Permissions { get; set; } = new List<DmsDocumentPermission>();
    public ICollection<DmsDocumentEvent> Events { get; set; } = new List<DmsDocumentEvent>();
    public ICollection<DmsLegalHold> LegalHolds { get; set; } = new List<DmsLegalHold>();
    public ICollection<DmsDocumentSignature> Signatures { get; set; } = new List<DmsDocumentSignature>();
}

/// <summary>
/// Preserved Historical or Current Version of a Document.
/// Mapped to dbo.DMS_DocumentVersions.
/// </summary>
public sealed class DmsDocumentVersion
{
    public long DocumentVersionId { get; set; }
    public long DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string VersionLabel { get; set; } = "v1.0";
    public long StoredObjectId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string FileExtension { get; set; } = ".pdf";
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string? ChangeComments { get; set; }
    public string? ContentSnapshotJson { get; set; }
    public bool IsCurrentVersion { get; set; }
    public bool IsOfficialSnapshot { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    public DmsDocument Document { get; set; } = null!;
    public DmsStoredObject StoredObject { get; set; } = null!;
}

/// <summary>
/// Many-to-Many Polymorphic Link Between a Central Document and Any HRM Business Record.
/// Mapped to dbo.DMS_DocumentLinks.
/// </summary>
public sealed class DmsDocumentLink
{
    public long DocumentLinkId { get; set; }
    public long DocumentId { get; set; }
    public int OrganisationId { get; set; }
    public string TargetEntityType { get; set; } = string.Empty; // 'Staff', 'Organisation', 'Department', 'Request', 'Transfer', 'Asset', 'AssetIssuance', 'Project', 'ProjectTask', 'Evaluation', 'CertificateRequest', 'IssuedCertificate', 'Position', 'Leave'
    public long TargetEntityId { get; set; }
    public string? TargetReferenceCode { get; set; }
    public string? TargetDisplayName { get; set; }
    public string LinkRelationship { get; set; } = "Primary"; // 'Primary', 'SupportingEvidence', 'GeneratedOutput', 'SignedCopy', 'PolicyReference', 'HandoverAttachment'
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
    public int LinkedByUserId { get; set; }
    public string LinkedByUserName { get; set; } = string.Empty;

    public DmsDocument Document { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
}

/// <summary>
/// Granular Explicit Access Control Entry (ACE) on a Document.
/// Mapped to dbo.DMS_DocumentPermissions.
/// </summary>
public sealed class DmsDocumentPermission
{
    public long DocumentPermissionId { get; set; }
    public long DocumentId { get; set; }
    public int OrganisationId { get; set; }
    public string PrincipalType { get; set; } = "User"; // 'User', 'Individual', 'Role', 'UserGroup', 'Department'
    public int? PrincipalId { get; set; }
    public string? PrincipalName { get; set; }
    public string PermissionLevel { get; set; } = "Preview"; // 'ViewMetadata', 'Preview', 'Download', 'Contribute', 'Approve', 'Manage', 'Deny'
    public DateTime? ExpiresAtUtc { get; set; }
    public string? GrantReason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public int GrantedByUserId { get; set; }
    public string GrantedByUserName { get; set; } = string.Empty;

    public DmsDocument Document { get; set; } = null!;
}

/// <summary>
/// Immutable Audit Trail for Document Access, Lifecycle, Security, and Governance Events.
/// Mapped to dbo.DMS_DocumentEvents.
/// </summary>
public sealed class DmsDocumentEvent
{
    public long DocumentEventId { get; set; }
    public long DocumentId { get; set; }
    public long? DocumentVersionId { get; set; }
    public int OrganisationId { get; set; }
    public string EventType { get; set; } = string.Empty; // 'Registered', 'Uploaded', 'VersionAdded', 'Previewed', 'Downloaded', 'MetadataUpdated', 'CheckedOut', 'CheckedIn', 'Linked', 'Unlinked', 'Verified', 'SubmittedForApproval', 'Approved', 'Rejected', 'Published', 'Signed', 'Shared', 'LegalHoldPlaced', 'LegalHoldReleased', 'Archived', 'Disposed', 'Restored'
    public string EventDescription { get; set; } = string.Empty;
    public string? PreviousStateJson { get; set; }
    public string? NewStateJson { get; set; }
    public int PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public DmsDocument Document { get; set; } = null!;
}

/// <summary>
/// Versioned Configurable Document Template with Safe Verified HRM Placeholders.
/// Mapped to dbo.DMS_DocumentTemplates.
/// </summary>
public sealed class DmsDocumentTemplate
{
    public int DocumentTemplateId { get; set; }
    public int? OrganisationId { get; set; }
    public int? CategoryId { get; set; }
    public int? DmsDocumentTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string DocumentTitlePattern { get; set; } = string.Empty;
    public string? HeaderText { get; set; }
    public string BodyTemplate { get; set; } = string.Empty; // Safe placeholder syntax {{EmployeeName}}, {{PositionTitle}}, etc.
    public string? FooterText { get; set; }
    public string? SignatoryName { get; set; }
    public string? SignatoryTitle { get; set; }
    public bool IncludeOrganisationLetterhead { get; set; } = true;
    public bool IncludePositionHistoryTable { get; set; }
    public bool IncludeQrVerification { get; set; } = true;
    public bool IsEmployeeRequestable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Organisation? Organisation { get; set; }
    public DmsCategory? Category { get; set; }
    public DmsDocumentType? DocumentType { get; set; }
}

/// <summary>
/// Employee / HR Official Document Request Integrated with Central Requests & Workflow Engine.
/// Mapped to dbo.DMS_DocumentRequests.
/// </summary>
public sealed class DmsDocumentRequest
{
    public int DocumentRequestId { get; set; }
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int ApplicantIndividualId { get; set; }
    public int? SubjectJobId { get; set; }
    public int? DmsDocumentTypeId { get; set; }
    public int? DocumentTemplateId { get; set; }
    public string RequestKind { get; set; } = "OfficialLetter"; // 'EmploymentVerificationLetter', 'ServiceCertificate', 'DetailedExperienceCertificate', 'SalaryConfirmationLetter', 'NoObjectionLetter', 'CustomOfficialDocument'
    public string Purpose { get; set; } = string.Empty;
    public string? AddressedTo { get; set; } = "To Whom It May Concern";
    public string? EmployeeNotes { get; set; }
    public bool IncludePositionHistory { get; set; }
    public bool IncludeSalaryDetails { get; set; }
    public string Status { get; set; } = "Submitted"; // 'Submitted', 'UnderReview', 'ReturnedForCorrection', 'Approved', 'Issued', 'Rejected', 'Cancelled'
    public string? ResolvedSnapshotJson { get; set; }
    public string? GeneratedBodyText { get; set; }
    public long? IssuedDocumentId { get; set; }
    public string? IssuedDocumentNumber { get; set; }
    public string? VerificationToken { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewComments { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Request Request { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public Individual ApplicantIndividual { get; set; } = null!;
    public DmsDocumentType? DocumentType { get; set; }
    public DmsDocumentTemplate? DocumentTemplate { get; set; }
    public DmsDocument? IssuedDocument { get; set; }
}

/// <summary>
/// Legal Hold Blocking Disposition, Deletion, or Modification of Protected Records.
/// Mapped to dbo.DMS_LegalHolds.
/// </summary>
public sealed class DmsLegalHold
{
    public int LegalHoldId { get; set; }
    public long DocumentId { get; set; }
    public int OrganisationId { get; set; }
    public string HoldReference { get; set; } = string.Empty;
    public string CaseOrMatterName { get; set; } = string.Empty;
    public string HoldReason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime PlacedAtUtc { get; set; } = DateTime.UtcNow;
    public int PlacedByUserId { get; set; }
    public string PlacedByUserName { get; set; } = string.Empty;
    public DateTime? ReleasedAtUtc { get; set; }
    public int? ReleasedByUserId { get; set; }
    public string? ReleasedByUserName { get; set; }
    public string? ReleaseNotes { get; set; }

    public DmsDocument Document { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
}

/// <summary>
/// Digital Approval / Sign-off & Cryptographic Digest Record on a Document Version.
/// Mapped to dbo.DMS_DocumentSignatures.
/// </summary>
public sealed class DmsDocumentSignature
{
    public long DocumentSignatureId { get; set; }
    public long DocumentId { get; set; }
    public long DocumentVersionId { get; set; }
    public int OrganisationId { get; set; }
    public int SignerUserId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public string SignatureRole { get; set; } = "Approver"; // 'Author', 'Verifier', 'Reviewer', 'Approver', 'IssuingAuthority', 'EmployeeAcknowledgement'
    public string SignatureStatus { get; set; } = "Signed"; // 'Pending', 'Signed', 'Declined', 'Revoked'
    public string ContentSha256AtSigning { get; set; } = string.Empty;
    public string SignatureDigest { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime SignedAtUtc { get; set; } = DateTime.UtcNow;

    public DmsDocument Document { get; set; } = null!;
    public DmsDocumentVersion DocumentVersion { get; set; } = null!;
}
