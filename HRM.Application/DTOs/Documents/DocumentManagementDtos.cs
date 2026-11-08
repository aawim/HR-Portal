using System;
using System.Collections.Generic;

namespace HRM.DTOs.Documents;

public sealed class DmsCategoryDto
{
    public int CategoryId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TargetDomain { get; set; } = "General";
    public int DisplayOrder { get; set; }
    public bool IsSystemCategory { get; set; }
    public bool IsActive { get; set; } = true;
    public int DocumentTypesCount { get; set; }
    public int DocumentsCount { get; set; }
}

public sealed class DmsDocumentTypeDto
{
    public int DmsDocumentTypeId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TargetDomain { get; set; } = "General";
    public int? LegacyDocumentTypeId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameDhivehi { get; set; }
    public string? Description { get; set; }
    public string DefaultClassification { get; set; } = "Internal";
    public string DefaultAccessScope { get; set; } = "EmployeeAndHr";
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
    public string? RetentionPolicyName { get; set; }
    public bool IsSystemType { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DmsRetentionPolicyDto
{
    public int RetentionPolicyId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RetentionMonths { get; set; }
    public string TriggerEvent { get; set; } = "CreationDate";
    public int? ArchiveAfterMonths { get; set; }
    public string DispositionAction { get; set; } = "PermanentKeep";
    public bool RequiresApprovalToDispose { get; set; } = true;
    public string? PolicyReferenceNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DmsEffectivePermissionsDto
{
    public bool CanViewMetadata { get; set; }
    public bool CanPreview { get; set; }
    public bool CanDownload { get; set; }
    public bool CanUpload { get; set; }
    public bool CanModifyMetadata { get; set; }
    public bool CanAddVersion { get; set; }
    public bool CanSubmitForApproval { get; set; }
    public bool CanReview { get; set; }
    public bool CanApprove { get; set; }
    public bool CanSign { get; set; }
    public bool CanShare { get; set; }
    public bool CanArchive { get; set; }
    public bool CanDispose { get; set; }
    public bool CanManageLegalHold { get; set; }
}

public sealed class DmsDocumentSummaryDto
{
    public long DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public int? DepartmentStructureId { get; set; }
    public string? DepartmentName { get; set; }
    public int? SubjectIndividualId { get; set; }
    public string? SubjectEmployeeName { get; set; }
    public string? SubjectStaffNumber { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TargetDomain { get; set; } = string.Empty;
    public int DmsDocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public string Classification { get; set; } = "Internal";
    public string AccessScope { get; set; } = "EmployeeAndHr";
    public string Status { get; set; } = "Active";
    public int CurrentVersionNumber { get; set; } = 1;
    public string CurrentFileName { get; set; } = string.Empty;
    public string CurrentFileExtension { get; set; } = string.Empty;
    public string CurrentContentType { get; set; } = string.Empty;
    public long CurrentFileSizeBytes { get; set; }
    public string CurrentSha256Hash { get; set; } = string.Empty;
    public bool IsOfficialRecord { get; set; }
    public bool IsImmutable { get; set; }
    public bool IsLockedForEdit { get; set; }
    public string? CheckedOutByUserName { get; set; }
    public bool IsVerified { get; set; }
    public string? VerifiedByUserName { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public bool HasActiveLegalHold { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? RetentionDueDateUtc { get; set; }
    public string? Tags { get; set; }
    public int LinksCount { get; set; }
    public int SignaturesCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
}

public sealed class DmsDocumentDetailDto
{
    public DmsDocumentSummaryDto Summary { get; set; } = new();
    public string? CustomMetadataJson { get; set; }
    public int? RetentionPolicyId { get; set; }
    public string? RetentionPolicyName { get; set; }
    public long? SupersededByDocumentId { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime? DisposedAtUtc { get; set; }
    public string? DisposalCertificateRef { get; set; }
    public DmsEffectivePermissionsDto EffectivePermissions { get; set; } = new();
    public List<DmsDocumentVersionDto> Versions { get; set; } = new();
    public List<DmsDocumentLinkDto> Links { get; set; } = new();
    public List<DmsDocumentPermissionDto> Permissions { get; set; } = new();
    public List<DmsDocumentSignatureDto> Signatures { get; set; } = new();
    public List<DmsLegalHoldDto> LegalHolds { get; set; } = new();
    public List<DmsDocumentEventDto> AuditEvents { get; set; } = new();
}

public sealed class DmsDocumentVersionDto
{
    public long DocumentVersionId { get; set; }
    public long DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string? ChangeComments { get; set; }
    public bool IsCurrentVersion { get; set; }
    public bool IsOfficialSnapshot { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
}

public sealed class DmsDocumentLinkDto
{
    public long DocumentLinkId { get; set; }
    public long DocumentId { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public long TargetEntityId { get; set; }
    public string? TargetReferenceCode { get; set; }
    public string? TargetDisplayName { get; set; }
    public string LinkRelationship { get; set; } = "Primary";
    public string? Notes { get; set; }
    public DateTime LinkedAtUtc { get; set; }
    public string LinkedByUserName { get; set; } = string.Empty;
}

public sealed class DmsDocumentPermissionDto
{
    public long DocumentPermissionId { get; set; }
    public long DocumentId { get; set; }
    public string PrincipalType { get; set; } = "User";
    public int? PrincipalId { get; set; }
    public string? PrincipalName { get; set; }
    public string PermissionLevel { get; set; } = "Preview";
    public DateTime? ExpiresAtUtc { get; set; }
    public string? GrantReason { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public string GrantedByUserName { get; set; } = string.Empty;
}

public sealed class DmsDocumentSignatureDto
{
    public long DocumentSignatureId { get; set; }
    public long DocumentId { get; set; }
    public long DocumentVersionId { get; set; }
    public int VersionNumber { get; set; }
    public int SignerUserId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public string SignatureRole { get; set; } = string.Empty;
    public string SignatureStatus { get; set; } = string.Empty;
    public string ContentSha256AtSigning { get; set; } = string.Empty;
    public string SignatureDigest { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime SignedAtUtc { get; set; }
}

public sealed class DmsLegalHoldDto
{
    public int LegalHoldId { get; set; }
    public long DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string HoldReference { get; set; } = string.Empty;
    public string CaseOrMatterName { get; set; } = string.Empty;
    public string HoldReason { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime PlacedAtUtc { get; set; }
    public string PlacedByUserName { get; set; } = string.Empty;
    public DateTime? ReleasedAtUtc { get; set; }
    public string? ReleasedByUserName { get; set; }
    public string? ReleaseNotes { get; set; }
}

public sealed class DmsDocumentEventDto
{
    public long DocumentEventId { get; set; }
    public long DocumentId { get; set; }
    public long? DocumentVersionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public string PerformedByUserName { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
}

public sealed class DmsDocumentSearchFilterDto
{
    public int? OrganisationId { get; set; }
    public string? SearchText { get; set; }
    public int? CategoryId { get; set; }
    public int? DmsDocumentTypeId { get; set; }
    public string? TargetDomain { get; set; }
    public int? SubjectIndividualId { get; set; }
    public int? DepartmentStructureId { get; set; }
    public string? Classification { get; set; }
    public string? Status { get; set; }
    public string? LinkedEntityType { get; set; }
    public long? LinkedEntityId { get; set; }
    public bool? OnlyOfficialRecords { get; set; }
    public bool? OnlyLegalHold { get; set; }
    public bool IncludeArchived { get; set; }
}

public sealed class DmsDocumentUploadInput
{
    public int? OrganisationId { get; set; }
    public int DmsDocumentTypeId { get; set; }
    public int? SubjectIndividualId { get; set; }
    public int? DepartmentStructureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Classification { get; set; }
    public string? AccessScope { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? EffectiveFromDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Tags { get; set; }
    public string? CustomMetadataJson { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string? VersionComments { get; set; }
    public bool SubmitImmediatelyForApproval { get; set; }
    public string? PrimaryLinkEntityType { get; set; }
    public long? PrimaryLinkEntityId { get; set; }
    public string? PrimaryLinkReferenceCode { get; set; }
    public string? PrimaryLinkDisplayName { get; set; }
}

public sealed class DmsDocumentNewVersionInput
{
    public long DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string? ChangeComments { get; set; }
    public bool CheckInAfterUpload { get; set; } = true;
}

public sealed class DmsDocumentMetadataUpdateInput
{
    public long DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public string Classification { get; set; } = "Internal";
    public string AccessScope { get; set; } = "EmployeeAndHr";
    public int? DepartmentStructureId { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? EffectiveFromDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Tags { get; set; }
    public string? CustomMetadataJson { get; set; }
    public int? RetentionPolicyId { get; set; }
}

public sealed class DmsDocumentLinkInput
{
    public long DocumentId { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public long TargetEntityId { get; set; }
    public string? TargetReferenceCode { get; set; }
    public string? TargetDisplayName { get; set; }
    public string LinkRelationship { get; set; } = "Primary";
    public string? Notes { get; set; }
}

public sealed class DmsDocumentPermissionInput
{
    public long DocumentId { get; set; }
    public string PrincipalType { get; set; } = "User"; // 'User', 'Individual', 'Role', 'UserGroup', 'Department'
    public int? PrincipalId { get; set; }
    public string? PrincipalName { get; set; }
    public string PermissionLevel { get; set; } = "Preview"; // 'ViewMetadata', 'Preview', 'Download', 'Contribute', 'Approve', 'Manage', 'Deny'
    public DateTime? ExpiresAtUtc { get; set; }
    public string? GrantReason { get; set; }
}

public sealed class DmsDocumentLifecycleActionInput
{
    public long DocumentId { get; set; }
    public string Action { get; set; } = string.Empty; // 'CheckOut', 'CheckIn', 'Verify', 'SubmitForApproval', 'Approve', 'Publish', 'Reject', 'Sign', 'Archive', 'Dispose', 'Revoke', 'RestoreVersion'
    public string? Comments { get; set; }
    public string? SignerTitle { get; set; }
    public string? SignatureRole { get; set; }
    public int? TargetVersionNumber { get; set; }
    public long? SupersededByDocumentId { get; set; }
}

public sealed class DmsLegalHoldInput
{
    public long DocumentId { get; set; }
    public int? LegalHoldId { get; set; }
    public bool Release { get; set; }
    public string HoldReference { get; set; } = string.Empty;
    public string CaseOrMatterName { get; set; } = string.Empty;
    public string HoldReason { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
}

public sealed class DmsDocumentDownloadDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public bool IntegrityVerified { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public sealed class DmsDocumentTemplateDto
{
    public int DocumentTemplateId { get; set; }
    public int? OrganisationId { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? DmsDocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string DocumentTitlePattern { get; set; } = string.Empty;
    public string? HeaderText { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public string? FooterText { get; set; }
    public string? SignatoryName { get; set; }
    public string? SignatoryTitle { get; set; }
    public bool IncludeOrganisationLetterhead { get; set; } = true;
    public bool IncludePositionHistoryTable { get; set; }
    public bool IncludeQrVerification { get; set; } = true;
    public bool IsEmployeeRequestable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class DmsTemplatePreviewDto
{
    public int DocumentTemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string IssueDateFormatted { get; set; } = string.Empty;
    public string ResolvedTitle { get; set; } = string.Empty;
    public string? HeaderText { get; set; }
    public string ResolvedBodyText { get; set; } = string.Empty;
    public string? FooterText { get; set; }
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryTitle { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public string EmployeeNationalId { get; set; } = string.Empty;
    public string StaffNumber { get; set; } = string.Empty;
    public string CurrentPositionTitle { get; set; } = string.Empty;
    public string CurrentDepartmentName { get; set; } = string.Empty;
    public string EmploymentStartDate { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public bool IncludeOrganisationLetterhead { get; set; } = true;
    public bool IncludePositionHistoryTable { get; set; }
    public bool IncludeQrVerification { get; set; } = true;
    public List<DmsTemplatePositionRowDto> PositionHistory { get; set; } = new();
}

public sealed class DmsTemplatePositionRowDto
{
    public string PositionTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

public sealed class DmsDocumentRequestDto
{
    public int DocumentRequestId { get; set; }
    public int RequestId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public int ApplicantIndividualId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantStaffNumber { get; set; } = string.Empty;
    public string ApplicantPositionTitle { get; set; } = string.Empty;
    public int? DmsDocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public int? DocumentTemplateId { get; set; }
    public string? TemplateName { get; set; }
    public string RequestKind { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? AddressedTo { get; set; }
    public string? EmployeeNotes { get; set; }
    public bool IncludePositionHistory { get; set; }
    public bool IncludeSalaryDetails { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResolvedSnapshotJson { get; set; }
    public string? GeneratedBodyText { get; set; }
    public long? IssuedDocumentId { get; set; }
    public string? IssuedDocumentNumber { get; set; }
    public string? VerificationToken { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewComments { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class DmsDocumentRequestSubmitInput
{
    public int? ApplicantIndividualId { get; set; }
    public int? DocumentTemplateId { get; set; }
    public int? DmsDocumentTypeId { get; set; }
    public string RequestKind { get; set; } = "EmploymentVerificationLetter";
    public string Purpose { get; set; } = string.Empty;
    public string? AddressedTo { get; set; } = "To Whom It May Concern";
    public string? EmployeeNotes { get; set; }
    public bool IncludePositionHistory { get; set; }
    public bool IncludeSalaryDetails { get; set; }
}

public sealed class DmsDocumentRequestActionInput
{
    public int DocumentRequestId { get; set; }
    public string Action { get; set; } = string.Empty; // 'StartReview', 'ReturnForCorrection', 'ApproveAndIssue', 'Reject', 'Cancel'
    public string? Comments { get; set; }
    public string? SignatoryName { get; set; }
    public string? SignatoryTitle { get; set; }
    public string? CustomBodyOverride { get; set; }
    public byte[]? GeneratedPdfBytes { get; set; }
}

public sealed class DmsDashboardSummaryDto
{
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int StaffDocumentsCount { get; set; }
    public int OrganisationLibraryCount { get; set; }
    public int OfficialRecordsCount { get; set; }
    public int PendingVerificationCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int PendingDocumentRequestsCount { get; set; }
    public int ActiveLegalHoldsCount { get; set; }
    public int ArchivedDocumentsCount { get; set; }
    public int RetentionDueCount { get; set; }
    public long TotalStorageBytes { get; set; }
    public List<DmsCategoryDto> Categories { get; set; } = new();
    public List<DmsDocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<DmsRetentionPolicyDto> RetentionPolicies { get; set; } = new();
    public List<DmsDocumentTemplateDto> Templates { get; set; } = new();
    public List<DmsDocumentSummaryDto> RecentDocuments { get; set; } = new();
}
