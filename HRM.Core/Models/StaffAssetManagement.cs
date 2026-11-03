using HRM.Enums;
using HRM.Models.Archives;

namespace HRM.Models;

/// <summary>
/// Type-level policy for serialized organizational assets (laptops, monitors, phones, cards, equipment).
/// Mapped to dbo.AM_AssetCategories.
/// </summary>
public sealed class AssetCategory
{
    public int AssetCategoryId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresSerialNumber { get; set; } = true;
    public bool RequiresReturn { get; set; } = true;
    public bool IsSecurityItem { get; set; }
    public bool IsActive { get; set; } = true;
    public int? OperationLogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Organisation? Organisation { get; set; }
    public OperationLog? OperationLog { get; set; }
    public ICollection<AssetItem> Assets { get; set; } = new List<AssetItem>();
}

/// <summary>
/// Condition dictionary (New, Good, Fair, Damaged, Unserviceable).
/// Mapped to dbo.AM_AssetConditions.
/// </summary>
public sealed class AssetCondition
{
    public int AssetConditionId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresMaintenanceOnReturn { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Single serialized physical asset item in the organizational asset register.
/// Mapped to dbo.AM_AssetItems.
/// </summary>
public sealed class AssetItem
{
    public int AssetItemId { get; set; }
    public int CategoryId { get; set; }
    public string AssetCode { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string Name { get; set; } = "";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int OwnerOrganisationId { get; set; }
    public int? OwnerStructureId { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = AssetInventoryStatuses.Available;
    public int CurrentConditionId { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? DefaultAccessories { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public int? OperationLogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AssetCategory Category { get; set; } = null!;
    public Organisation OwnerOrganisation { get; set; } = null!;
    public OrganisationStructure? OwnerStructure { get; set; }
    public AssetCondition CurrentCondition { get; set; } = null!;
    public OperationLog? OperationLog { get; set; }

    public ICollection<AssetIssuanceItem> IssuanceItems { get; set; } = new List<AssetIssuanceItem>();
    public ICollection<AssetCustody> Custodies { get; set; } = new List<AssetCustody>();
    public ICollection<AssetEvent> Events { get; set; } = new List<AssetEvent>();
}

/// <summary>
/// Multi-item asset issuance request header tied to IndividualID, JobID, OrganisationID and optional StructureID.
/// Mapped to dbo.AM_AssetIssuances.
/// </summary>
public sealed class AssetIssuance
{
    public int AssetIssuanceId { get; set; }
    public int? RequestId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int? StructureId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = AssetIssuanceStatuses.Draft;
    public int CreatedByUserId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public int? FinalizedByUserId { get; set; }
    public string? FinalizationIdempotencyKey { get; set; }

    // Immutable staff & job metadata snapshots captured at issuance for historical/PDF stability
    public string EmployeeNameSnapshot { get; set; } = "";
    public string StaffNumberSnapshot { get; set; } = "";
    public string PositionTitleSnapshot { get; set; } = "";
    public string DepartmentNameSnapshot { get; set; } = "";
    public string OrganisationNameSnapshot { get; set; } = "";

    public int? OperationLogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Request? Request { get; set; }
    public Individual Individual { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public OrganisationStructure? Structure { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public User? FinalizedByUser { get; set; }
    public OperationLog? OperationLog { get; set; }

    public ICollection<AssetIssuanceItem> Lines { get; set; } = new List<AssetIssuanceItem>();
    public ICollection<AssetDocument> Documents { get; set; } = new List<AssetDocument>();
}

/// <summary>
/// Distinct physical serialized item line in an issuance, tracking reservation, physical handover, version, and line status.
/// Mapped to dbo.AM_AssetIssuanceItems.
/// </summary>
public sealed class AssetIssuanceItem
{
    public int AssetIssuanceItemId { get; set; }
    public int IssuanceId { get; set; }
    public int AssetItemId { get; set; }
    public int ConditionAtIssueId { get; set; }
    public string? Accessories { get; set; }
    public DateTime? HandedOverAtUtc { get; set; }
    public int? HandedOverByUserId { get; set; }
    public string? HandoverNotes { get; set; }
    public string Status { get; set; } = AssetIssuanceLineStatuses.Proposed;
    public bool IsActiveReservation { get; set; }
    public int? ReplacedFromIssuanceItemId { get; set; }
    public int Version { get; set; } = 1;
    public DateTime? ExpectedReturnDate { get; set; }

    // Immutable item snapshots captured at issue/handover so later master edits do not alter historical evidence
    public string AssetCodeSnapshot { get; set; } = "";
    public string? SerialNumberSnapshot { get; set; }
    public string CategoryNameSnapshot { get; set; } = "";
    public string ItemNameSnapshot { get; set; } = "";
    public string? BrandSnapshot { get; set; }
    public string? ModelSnapshot { get; set; }
    public string ConditionNameSnapshot { get; set; } = "";
    public bool IsSecurityItemSnapshot { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public AssetIssuance Issuance { get; set; } = null!;
    public AssetItem AssetItem { get; set; } = null!;
    public AssetCondition ConditionAtIssue { get; set; } = null!;
    public User? HandedOverByUser { get; set; }
    public AssetIssuanceItem? ReplacedFromIssuanceItem { get; set; }

    public ICollection<AssetAcknowledgement> Acknowledgements { get; set; } = new List<AssetAcknowledgement>();
    public ICollection<AssetHrDecision> HrDecisions { get; set; } = new List<AssetHrDecision>();
    public AssetCustody? Custody { get; set; }
}

/// <summary>
/// Append-only employee accept/reject decision per issuance line and version.
/// Mapped to dbo.AM_AssetAcknowledgements.
/// </summary>
public sealed class AssetAcknowledgement
{
    public int AcknowledgementId { get; set; }
    public int IssuanceItemId { get; set; }
    public int IndividualId { get; set; }
    public string Decision { get; set; } = AssetStaffDecisions.Accepted;
    public string? RejectionReasonCode { get; set; }
    public string? Remarks { get; set; }
    public string AcknowledgementStatement { get; set; } = "";
    public int? EvidenceAttachmentId { get; set; }
    public int ConfirmedByUserId { get; set; }
    public DateTime ConfirmedAtUtc { get; set; }
    public int ItemVersion { get; set; }

    public AssetIssuanceItem IssuanceItem { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public User ConfirmedByUser { get; set; } = null!;
}

/// <summary>
/// Append-only HR item-level review decision (HRApproved, ReturnedForCorrection, Closed).
/// Mapped to dbo.AM_AssetHrDecisions.
/// </summary>
public sealed class AssetHrDecision
{
    public int HrDecisionId { get; set; }
    public int IssuanceItemId { get; set; }
    public string Decision { get; set; } = AssetHrDecisionKinds.HRApproved;
    public string? Comments { get; set; }
    public int DecidedByUserId { get; set; }
    public DateTime DecidedAtUtc { get; set; }
    public int ItemVersion { get; set; }
    public string IdempotencyKey { get; set; } = "";

    public AssetIssuanceItem IssuanceItem { get; set; } = null!;
    public User DecidedByUser { get; set; } = null!;
}

/// <summary>
/// Effective-dated active and historical custody assignment for a physical asset.
/// Mapped to dbo.AM_AssetCustodies.
/// </summary>
public sealed class AssetCustody
{
    public int AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int? StructureId { get; set; }
    public int IssuanceItemId { get; set; }
    public DateTime CustodyFromUtc { get; set; }
    public DateTime? CustodyToUtc { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string Status { get; set; } = AssetCustodyStatuses.Active;
    public string? ClosedReason { get; set; }
    public int? ClosedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public byte[] RowVersion { get; set; } = [];

    public AssetItem AssetItem { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public OrganisationStructure? Structure { get; set; }
    public AssetIssuanceItem IssuanceItem { get; set; } = null!;
    public User? ClosedByUser { get; set; }

    public ICollection<AssetReturnItem> ReturnItems { get; set; } = new List<AssetReturnItem>();
    public ICollection<AssetTransferDisposition> TransferDispositions { get; set; } = new List<AssetTransferDisposition>();
}

/// <summary>
/// Return / damage / loss / clearance transaction header.
/// Mapped to dbo.AM_AssetReturns.
/// </summary>
public sealed class AssetReturn
{
    public int AssetReturnId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int? JobId { get; set; }
    public int? StaffTransferId { get; set; }
    public string ReturnType { get; set; } = AssetReturnTypes.StandardReturn;
    public int RequestedByUserId { get; set; }
    public int? ReturnedToUserId { get; set; }
    public DateTime ReturnDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = AssetReturnStatuses.ReturnPending;
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Organisation Organisation { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public Job? Job { get; set; }
    public StaffTransfer? StaffTransfer { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public User? ReturnedToUser { get; set; }

    public ICollection<AssetReturnItem> Items { get; set; } = new List<AssetReturnItem>();
}

/// <summary>
/// Returned asset line, inspection outcome, missing accessories, and post-return disposition.
/// Mapped to dbo.AM_AssetReturnItems.
/// </summary>
public sealed class AssetReturnItem
{
    public int AssetReturnItemId { get; set; }
    public int AssetReturnId { get; set; }
    public int AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public int? ConditionAtReturnId { get; set; }
    public string? AccessoryChecklist { get; set; }
    public string? MissingAccessories { get; set; }
    public string? DamageOrLossEvidence { get; set; }
    public string InspectionResult { get; set; } = AssetInspectionResults.Pending;
    public string? PostReturnDisposition { get; set; }
    public int? ReceivedByUserId { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public string Status { get; set; } = AssetReturnStatuses.ReturnPending;

    public AssetReturn AssetReturn { get; set; } = null!;
    public AssetCustody AssetCustody { get; set; } = null!;
    public AssetItem AssetItem { get; set; } = null!;
    public AssetCondition? ConditionAtReturn { get; set; }
    public User? ReceivedByUser { get; set; }
}

/// <summary>
/// Internal job/department transfer asset disposition (Retain, Return, ChangeOwningDepartment, Replace).
/// Mapped to dbo.AM_AssetTransferDispositions.
/// </summary>
public sealed class AssetTransferDisposition
{
    public int AssetTransferDispositionId { get; set; }
    public int? StaffTransferId { get; set; }
    public int AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public int IndividualId { get; set; }
    public int FromJobId { get; set; }
    public int? ToJobId { get; set; }
    public int FromOrganisationId { get; set; }
    public int ToOrganisationId { get; set; }
    public int? FromStructureId { get; set; }
    public int? ToStructureId { get; set; }
    public string Decision { get; set; } = AssetTransferDecisions.Retain;
    public string Status { get; set; } = "Pending";
    public int DecidedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime DecidedAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public string? Remarks { get; set; }

    public StaffTransfer? StaffTransfer { get; set; }
    public AssetCustody AssetCustody { get; set; } = null!;
    public AssetItem AssetItem { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public Job FromJob { get; set; } = null!;
    public Job? ToJob { get; set; }
    public Organisation FromOrganisation { get; set; } = null!;
    public Organisation ToOrganisation { get; set; } = null!;
    public OrganisationStructure? FromStructure { get; set; }
    public OrganisationStructure? ToStructure { get; set; }
    public User DecidedByUser { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
}

/// <summary>
/// Immutable append-only audit timeline for asset, issuance, acknowledgement, approval, custody, return, and document events.
/// Mapped to dbo.AM_AssetEvents.
/// </summary>
public sealed class AssetEvent
{
    public long AssetEventId { get; set; }
    public int OrganisationId { get; set; }
    public int? AssetItemId { get; set; }
    public string RelatedEntityType { get; set; } = "";
    public int RelatedEntityId { get; set; }
    public string EventType { get; set; } = "";
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public int ActorUserId { get; set; }
    public DateTime AtUtc { get; set; }
    public string? MetadataJson { get; set; }

    public Organisation Organisation { get; set; } = null!;
    public AssetItem? AssetItem { get; set; }
    public User ActorUser { get; set; } = null!;
}

/// <summary>
/// Controlled file metadata and checksum for handover photos, damage evidence, and attachments.
/// Mapped to dbo.AM_AssetAttachments.
/// </summary>
public sealed class AssetAttachment
{
    public int AttachmentId { get; set; }
    public int OrganisationId { get; set; }
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string StorageKey { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
    public byte[]? FileBytes { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string AccessScope { get; set; } = AssetAttachmentScopes.EmployeeAndHr;

    public Organisation Organisation { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}

/// <summary>
/// Immutable approved A4 Asset Handover & Acknowledgement document snapshot, version, and SHA-256 hash.
/// Mapped to dbo.AM_AssetDocuments.
/// </summary>
public sealed class AssetDocument
{
    public long AssetDocumentId { get; set; }
    public int AssetIssuanceId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentVariant { get; set; } = AssetDocumentVariants.StandardHandoverRecord;
    public int DocumentVersion { get; set; } = 1;
    public string SnapshotJson { get; set; } = "";
    public string Sha256Hash { get; set; } = "";
    public byte[]? PdfBytes { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public int GeneratedByUserId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    public AssetIssuance AssetIssuance { get; set; } = null!;
    public User GeneratedByUser { get; set; } = null!;
}
