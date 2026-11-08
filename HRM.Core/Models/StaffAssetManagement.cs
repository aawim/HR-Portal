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
    public ICollection<AssetComponent> Components { get; set; } = new List<AssetComponent>();
    public ICollection<AssetMaintenanceJob> MaintenanceJobs { get; set; } = new List<AssetMaintenanceJob>();
    public ICollection<AssetTemporaryHandover> PrimaryHandovers { get; set; } = new List<AssetTemporaryHandover>();
    public ICollection<AssetTemporaryHandover> LoanerHandovers { get; set; } = new List<AssetTemporaryHandover>();
    public ICollection<AssetReplacementLink> OriginalReplacementLinks { get; set; } = new List<AssetReplacementLink>();
    public ICollection<AssetReplacementLink> NewReplacementLinks { get; set; } = new List<AssetReplacementLink>();
    public ICollection<AssetInspection> Inspections { get; set; } = new List<AssetInspection>();
    public ICollection<AssetConfigurationSnapshot> ConfigurationSnapshots { get; set; } = new List<AssetConfigurationSnapshot>();
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
    public ICollection<AssetServiceRequestItem> ServiceRequestItems { get; set; } = new List<AssetServiceRequestItem>();
    public ICollection<AssetReplacementLink> OriginalReplacementLinks { get; set; } = new List<AssetReplacementLink>();
    public ICollection<AssetReplacementLink> NewReplacementLinks { get; set; } = new List<AssetReplacementLink>();
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

/// <summary>
/// Service, fault repair, hardware upgrade, replacement, and clearance request header.
/// Mapped to dbo.AM_AssetServiceRequests.
/// </summary>
public sealed class AssetServiceRequest
{
    public int AssetServiceRequestId { get; set; }
    public int? RequestId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int? StructureId { get; set; }
    public int? StaffTransferId { get; set; }
    public string RequestType { get; set; } = AssetServiceRequestTypes.FaultCorrective;
    public string Priority { get; set; } = "Standard";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = AssetServiceRequestStatuses.Submitted;
    public int RequestedByUserId { get; set; }
    public int? AssignedTechnicianUserId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResolutionSummary { get; set; }
    public int? OperationLogId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Request? Request { get; set; }
    public Individual Individual { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public OrganisationStructure? Structure { get; set; }
    public StaffTransfer? StaffTransfer { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public User? AssignedTechnicianUser { get; set; }
    public OperationLog? OperationLog { get; set; }

    public ICollection<AssetServiceRequestItem> Items { get; set; } = new List<AssetServiceRequestItem>();
    public ICollection<AssetMaintenanceJob> MaintenanceJobs { get; set; } = new List<AssetMaintenanceJob>();
    public ICollection<AssetReplacementLink> ReplacementLinks { get; set; } = new List<AssetReplacementLink>();
    public ICollection<AssetTemporaryHandover> TemporaryHandovers { get; set; } = new List<AssetTemporaryHandover>();
    public ICollection<AssetInspection> Inspections { get; set; } = new List<AssetInspection>();
    public ICollection<AssetServiceDocument> ServiceDocuments { get; set; } = new List<AssetServiceDocument>();
}

/// <summary>
/// Individual items subject to service/upgrade/replacement within a service request.
/// Mapped to dbo.AM_AssetServiceRequestItems.
/// </summary>
public sealed class AssetServiceRequestItem
{
    public int AssetServiceRequestItemId { get; set; }
    public int AssetServiceRequestId { get; set; }
    public int? AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public string ItemRequestType { get; set; } = AssetServiceRequestTypes.FaultCorrective;
    public string ReportedFaultOrRequirement { get; set; } = "";
    public string? TargetComponentCategory { get; set; }
    public string? TargetSpecification { get; set; }
    public string Status { get; set; } = "Pending";
    public bool StaffAcknowledged { get; set; }
    public string? StaffDecision { get; set; }
    public string? StaffRemarks { get; set; }
    public DateTime? StaffConfirmedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AssetServiceRequest ServiceRequest { get; set; } = null!;
    public AssetCustody? AssetCustody { get; set; }
    public AssetItem AssetItem { get; set; } = null!;
}

/// <summary>
/// Technical repair, upgrade, or testing work order.
/// Mapped to dbo.AM_AssetMaintenanceJobs.
/// </summary>
public sealed class AssetMaintenanceJob
{
    public int AssetMaintenanceJobId { get; set; }
    public string WorkOrderNumber { get; set; } = "";
    public int? AssetServiceRequestId { get; set; }
    public int AssetItemId { get; set; }
    public string JobType { get; set; } = AssetMaintenanceJobTypes.Corrective;
    public string Priority { get; set; } = "Standard";
    public int AssignedTechnicianUserId { get; set; }
    public string Status { get; set; } = AssetMaintenanceJobStatuses.Assigned;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? DiagnosticSummary { get; set; }
    public string? WorkPerformedSummary { get; set; }
    public string? HardwareTestSummary { get; set; }
    public string? DataSanitizationMethod { get; set; }
    public bool DataSanitizationVerified { get; set; }
    public string? VendorWarrantyClaimNumber { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LabourCost { get; set; }
    public decimal TotalCost { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AssetServiceRequest? ServiceRequest { get; set; }
    public AssetItem AssetItem { get; set; } = null!;
    public User AssignedTechnicianUser { get; set; } = null!;

    public ICollection<AssetMaintenanceAction> Actions { get; set; } = new List<AssetMaintenanceAction>();
    public ICollection<AssetComponentTransaction> ComponentTransactions { get; set; } = new List<AssetComponentTransaction>();
    public ICollection<AssetServiceDocument> ServiceDocuments { get; set; } = new List<AssetServiceDocument>();
}

/// <summary>
/// Technician activity log and milestone action during servicing.
/// Mapped to dbo.AM_AssetMaintenanceActions.
/// </summary>
public sealed class AssetMaintenanceAction
{
    public long AssetMaintenanceActionId { get; set; }
    public int AssetMaintenanceJobId { get; set; }
    public string ActionType { get; set; } = "Diagnosis";
    public int TechnicianUserId { get; set; }
    public DateTime ActionAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string Remarks { get; set; } = "";
    public string? MetadataJson { get; set; }

    public AssetMaintenanceJob MaintenanceJob { get; set; } = null!;
    public User TechnicianUser { get; set; } = null!;
}

/// <summary>
/// Physical hardware component (RAM, SSD, battery, display, etc.) installed or stocked.
/// Mapped to dbo.AM_AssetComponents.
/// </summary>
public sealed class AssetComponent
{
    public int AssetComponentId { get; set; }
    public int OwnerOrganisationId { get; set; }
    public int? AssetItemId { get; set; }
    public string ComponentType { get; set; } = AssetComponentTypes.RAM;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string Specification { get; set; } = "";
    public string? CapacityOrRating { get; set; }
    public string? SlotOrPortLocation { get; set; }
    public string Status { get; set; } = AssetComponentStatuses.StockSpare;
    public decimal? Cost { get; set; }
    public DateTime? InstalledAtUtc { get; set; }
    public DateTime? RemovedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Organisation OwnerOrganisation { get; set; } = null!;
    public AssetItem? AssetItem { get; set; }

    public ICollection<AssetComponentTransaction> Transactions { get; set; } = new List<AssetComponentTransaction>();
}

/// <summary>
/// Immutable audit log of every component install, removal, upgrade, swap, and disposal.
/// Mapped to dbo.AM_AssetComponentTransactions.
/// </summary>
public sealed class AssetComponentTransaction
{
    public long AssetComponentTransactionId { get; set; }
    public int AssetComponentId { get; set; }
    public int AssetItemId { get; set; }
    public int? AssetMaintenanceJobId { get; set; }
    public string TransactionType { get; set; } = AssetComponentTransactionTypes.InstalledAsUpgrade;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = AssetComponentStatuses.Installed;
    public int TechnicianUserId { get; set; }
    public DateTime TransactedAtUtc { get; set; }
    public string? Notes { get; set; }

    public AssetComponent AssetComponent { get; set; } = null!;
    public AssetItem AssetItem { get; set; } = null!;
    public AssetMaintenanceJob? MaintenanceJob { get; set; }
    public User TechnicianUser { get; set; } = null!;
}

/// <summary>
/// Inventory of spare parts and replacement components stocked in the IT workshop.
/// Mapped to dbo.AM_AssetServiceParts.
/// </summary>
public sealed class AssetServicePart
{
    public int AssetServicePartId { get; set; }
    public int OrganisationId { get; set; }
    public string PartCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Specification { get; set; } = "";
    public string? CompatibleModels { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public decimal UnitCost { get; set; }
    public int ReorderLevel { get; set; } = 5;
    public string? LocationBin { get; set; }
    public bool IsActive { get; set; } = true;

    public Organisation Organisation { get; set; } = null!;
}

/// <summary>
/// Explicit link between an original device/accessory and its replacement.
/// Mapped to dbo.AM_AssetReplacementLinks.
/// </summary>
public sealed class AssetReplacementLink
{
    public int AssetReplacementLinkId { get; set; }
    public int? AssetServiceRequestId { get; set; }
    public int OriginalAssetItemId { get; set; }
    public int? OriginalAssetCustodyId { get; set; }
    public int ReplacementAssetItemId { get; set; }
    public int? ReplacementAssetCustodyId { get; set; }
    public string ReplacementType { get; set; } = AssetReplacementTypes.CompleteDeviceReplacement;
    public string ReplacementReason { get; set; } = AssetReplacementReasons.BeyondEconomicalRepair;
    public string OriginalAssetDisposition { get; set; } = "PendingReturn";
    public bool StaffAcknowledged { get; set; }
    public string? StaffDecision { get; set; }
    public string? StaffRemarks { get; set; }
    public DateTime? StaffConfirmedAtUtc { get; set; }
    public int RecordedByUserId { get; set; }
    public DateTime RecordedAtUtc { get; set; }

    public AssetServiceRequest? ServiceRequest { get; set; }
    public AssetItem OriginalAssetItem { get; set; } = null!;
    public AssetCustody? OriginalAssetCustody { get; set; }
    public AssetItem ReplacementAssetItem { get; set; } = null!;
    public AssetCustody? ReplacementAssetCustody { get; set; }
    public User RecordedByUser { get; set; } = null!;
}

/// <summary>
/// Temporary loaner device assigned while an employee's primary device is serviced.
/// Mapped to dbo.AM_AssetTemporaryHandovers.
/// </summary>
public sealed class AssetTemporaryHandover
{
    public int AssetTemporaryHandoverId { get; set; }
    public int? AssetServiceRequestId { get; set; }
    public int PrimaryAssetItemId { get; set; }
    public int LoanerAssetItemId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public DateTime HandedOverAtUtc { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnedAtUtc { get; set; }
    public int HandedOverByUserId { get; set; }
    public int? ReceivedBackByUserId { get; set; }
    public int ConditionOutId { get; set; }
    public int? ConditionInId { get; set; }
    public string Status { get; set; } = AssetTemporaryHandoverStatuses.ActiveLoaner;
    public bool StaffAcknowledgementConfirmed { get; set; }
    public DateTime? StaffConfirmedAtUtc { get; set; }
    public string? Notes { get; set; }

    public AssetServiceRequest? ServiceRequest { get; set; }
    public AssetItem PrimaryAssetItem { get; set; } = null!;
    public AssetItem LoanerAssetItem { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public User HandedOverByUser { get; set; } = null!;
    public User? ReceivedBackByUser { get; set; }
    public AssetCondition ConditionOut { get; set; } = null!;
    public AssetCondition? ConditionIn { get; set; }
}

/// <summary>
/// Technical and physical condition inspection record.
/// Mapped to dbo.AM_AssetInspections.
/// </summary>
public sealed class AssetInspection
{
    public int AssetInspectionId { get; set; }
    public int AssetItemId { get; set; }
    public int? AssetCustodyId { get; set; }
    public int? AssetServiceRequestId { get; set; }
    public int? AssetReturnItemId { get; set; }
    public string InspectionStage { get; set; } = AssetInspectionStages.PreServiceTriage;
    public int InspectorUserId { get; set; }
    public DateTime InspectedAtUtc { get; set; }
    public int OverallConditionId { get; set; }
    public string? PhysicalDamageReport { get; set; }
    public string? FunctionalTestSummary { get; set; }
    public string? AccessoriesChecklist { get; set; }
    public bool DataSanitizationVerified { get; set; }
    public string InspectorDecision { get; set; } = AssetInspectorDecisions.Passed;
    public string? Notes { get; set; }

    public AssetItem AssetItem { get; set; } = null!;
    public AssetCustody? AssetCustody { get; set; }
    public AssetServiceRequest? ServiceRequest { get; set; }
    public AssetReturnItem? ReturnItem { get; set; }
    public User InspectorUser { get; set; } = null!;
    public AssetCondition OverallCondition { get; set; } = null!;
}

/// <summary>
/// Immutable versioned service/upgrade/replacement/clearance report snapshot with SHA-256 hash.
/// Mapped to dbo.AM_AssetServiceDocuments.
/// </summary>
public sealed class AssetServiceDocument
{
    public long AssetServiceDocumentId { get; set; }
    public string DocumentType { get; set; } = AssetServiceDocumentTypes.ServiceReport;
    public string DocumentNumber { get; set; } = "";
    public int DocumentVersion { get; set; } = 1;
    public int? AssetServiceRequestId { get; set; }
    public int? AssetMaintenanceJobId { get; set; }
    public int? AssetItemId { get; set; }
    public int? IndividualId { get; set; }
    public string SnapshotJson { get; set; } = "";
    public string Sha256Hash { get; set; } = "";
    public int GeneratedByUserId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public bool IsRestrictedSecurityVariant { get; set; }

    public AssetServiceRequest? ServiceRequest { get; set; }
    public AssetMaintenanceJob? MaintenanceJob { get; set; }
    public AssetItem? AssetItem { get; set; }
    public Individual? Individual { get; set; }
    public User GeneratedByUser { get; set; } = null!;
}

/// <summary>
/// Complete historical configuration profile of an asset (CPU, RAM, storage, battery, installed parts).
/// Mapped to dbo.AM_AssetConfigurationSnapshots.
/// </summary>
public sealed class AssetConfigurationSnapshot
{
    public long AssetConfigurationSnapshotId { get; set; }
    public int AssetItemId { get; set; }
    public string TriggerEvent { get; set; } = AssetConfigurationTriggerEvents.InitialRegistration;
    public string? ProcessorSpec { get; set; }
    public string RamSummary { get; set; } = "";
    public string StorageSummary { get; set; } = "";
    public string? GraphicsSpec { get; set; }
    public int? BatteryHealthPercentage { get; set; }
    public string? OsVersion { get; set; }
    public string ComponentsJson { get; set; } = "";
    public DateTime CapturedAtUtc { get; set; }
    public int CapturedByUserId { get; set; }

    public AssetItem AssetItem { get; set; } = null!;
    public User CapturedByUser { get; set; } = null!;
}
