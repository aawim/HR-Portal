using HRM.DTOs.Organisation;
using HRM.Enums;

namespace HRM.DTOs.Assets;

public sealed class AssetDashboardData
{
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public bool IsSuperAdmin { get; set; }
    public bool CanManageInventory { get; set; }
    public bool CanInitiateIssuance { get; set; }
    public bool CanApproveHr { get; set; }
    public bool CanInspectReturns { get; set; }
    public bool CanViewAudit { get; set; }
    public bool CanExportRestrictedPdf { get; set; }

    // Solid KPI Card counts (Section 15: Blue Total, Green Assigned, Teal Available, Amber Staff Pending, Violet HR Pending, Rose Overdue Returns)
    public int TotalAssetsCount { get; set; }
    public int AssignedAssetsCount { get; set; }
    public int AvailableAssetsCount { get; set; }
    public int StaffPendingCount { get; set; }
    public int HrPendingCount { get; set; }
    public int OverdueReturnsCount { get; set; }

    public int ReservedOrHandedOverCount { get; set; }
    public int MaintenanceCount { get; set; }
    public int LostOrDisposedCount { get; set; }
    public int OverdueAcknowledgementsCount { get; set; }
    public int OpenClearanceCount { get; set; }

    public List<OrganisationSettingsOption> Organisations { get; set; } = [];
    public List<AssetDepartmentOptionDto> Departments { get; set; } = [];
    public List<AssetCategoryDto> Categories { get; set; } = [];
    public List<AssetConditionDto> Conditions { get; set; } = [];
    public List<AssetStaffOptionDto> StaffOptions { get; set; } = [];

    public List<AssetChartSliceDto> CategoryBreakdown { get; set; } = [];
    public List<AssetChartSliceDto> ConditionBreakdown { get; set; } = [];
    public List<AssetAlertRowDto> OverdueAndReconciliationAlerts { get; set; } = [];

    public List<AssetItemDto> Assets { get; set; } = [];
    public List<AssetIssuanceQueueRowDto> Issuances { get; set; } = [];
    public List<AssetCustodyRowDto> Custodies { get; set; } = [];
    public List<AssetReturnDetailDto> Returns { get; set; } = [];
    public List<AssetTransferDispositionDto> TransferDispositions { get; set; } = [];
    public List<AssetEventDto> RecentAuditEvents { get; set; } = [];

    // Lifecycle Management: Repairs, Work Orders, Loaners, Replacements
    public List<AssetServiceRequestSummaryDto> ServiceRequests { get; set; } = [];
    public List<AssetMaintenanceJobSummaryDto> MaintenanceJobs { get; set; } = [];
    public List<AssetTemporaryHandoverDto> ActiveLoaners { get; set; } = [];
    public List<AssetReplacementLinkDto> ReplacementLinks { get; set; } = [];
}

public sealed record AssetDepartmentOptionDto(int StructureId, int OrganisationId, string Name);

public sealed record AssetChartSliceDto(string Label, int Count, int AvailableCount, int AssignedCount, string BadgeTone);

public sealed record AssetAlertRowDto(
    string AlertKind,
    string Severity,
    string Reference,
    int? RelatedId,
    string AssetCode,
    string ItemSummary,
    int IndividualId,
    string StaffName,
    DateTime DueOrSinceLocal,
    string Description);

public sealed class AssetCategoryDto
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
    public int AssetCount { get; set; }
    public int AvailableCount { get; set; }
}

public sealed class AssetCategorySaveInput
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
}

public sealed class AssetConditionDto
{
    public int AssetConditionId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresMaintenanceOnReturn { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AssetItemDto
{
    public int AssetItemId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryCode { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public bool RequiresSerialNumber { get; set; }
    public bool RequiresReturn { get; set; }
    public bool IsSecurityItem { get; set; }
    public string AssetCode { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string Name { get; set; } = "";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int OwnerOrganisationId { get; set; }
    public string OwnerOrganisationName { get; set; } = "";
    public int? OwnerStructureId { get; set; }
    public string? OwnerStructureName { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = AssetInventoryStatuses.Available;
    public int CurrentConditionId { get; set; }
    public string CurrentConditionName { get; set; } = "";
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? DefaultAccessories { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ActiveCustodianIndividualId { get; set; }
    public string? ActiveCustodianName { get; set; }
    public DateTime? ActiveCustodyFromLocal { get; set; }
    public bool HasHistory { get; set; }
    public string RowVersionBase64 { get; set; } = "";
}

public sealed class AssetItemSaveInput
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
    public string? RowVersionBase64 { get; set; }
}

public sealed class AssetStaffOptionDto
{
    public int IndividualId { get; set; }
    public string EmployeeNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public int JobId { get; set; }
    public string PositionTitle { get; set; } = "";
    public int OrganisationId { get; set; }
    public int? StructureId { get; set; }
    public string DepartmentName { get; set; } = "";
    public bool HasTerminatingOrTransferNotice { get; set; }
}

public sealed class AssetIssuanceQueueRowDto
{
    public int AssetIssuanceId { get; set; }
    public int? RequestId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int IndividualId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public int JobId { get; set; }
    public string PositionTitle { get; set; } = "";
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public int? StructureId { get; set; }
    public string DepartmentName { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = AssetIssuanceStatuses.Draft;
    public DateTime IssueDate { get; set; }
    public DateTime UpdatedAtLocal { get; set; }
    public int TotalLines { get; set; }
    public int HandedOverLines { get; set; }
    public int AcceptedLines { get; set; }
    public int RejectedLines { get; set; }
    public int HrApprovedLines { get; set; }
    public bool IsOverdueAcknowledgement { get; set; }
}

public sealed class AssetIssuanceDetailDto
{
    public int AssetIssuanceId { get; set; }
    public int? RequestId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int IndividualId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public int JobId { get; set; }
    public string PositionTitle { get; set; } = "";
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public int? StructureId { get; set; }
    public string DepartmentName { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = AssetIssuanceStatuses.Draft;
    public int CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime CreatedAtLocal { get; set; }
    public DateTime UpdatedAtLocal { get; set; }
    public DateTime? SubmittedAtLocal { get; set; }
    public DateTime? FinalizedAtLocal { get; set; }
    public string RowVersionBase64 { get; set; } = "";

    public bool CanEditDraft { get; set; }
    public bool CanReserveAndHandOver { get; set; }
    public bool CanStaffAcknowledge { get; set; }
    public bool CanHrReview { get; set; }
    public bool CanCancel { get; set; }

    public List<AssetIssuanceLineDto> Lines { get; set; } = [];
    public List<AssetDocumentSummaryDto> Documents { get; set; } = [];
    public List<AssetEventDto> Events { get; set; } = [];
}

public sealed class AssetIssuanceLineDto
{
    public int AssetIssuanceItemId { get; set; }
    public int IssuanceId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string CategoryName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int ConditionAtIssueId { get; set; }
    public string ConditionName { get; set; } = "";
    public bool IsSecurityItem { get; set; }
    public string? Accessories { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? HandedOverAtLocal { get; set; }
    public string? HandedOverByName { get; set; }
    public string? HandoverNotes { get; set; }
    public string Status { get; set; } = AssetIssuanceLineStatuses.Proposed;
    public bool IsActiveReservation { get; set; }
    public int? ReplacedFromIssuanceItemId { get; set; }
    public int Version { get; set; } = 1;
    public string RowVersionBase64 { get; set; } = "";

    // Latest current-version decisions
    public string? StaffDecision { get; set; }
    public string? StaffRejectionReasonCode { get; set; }
    public string? StaffRemarks { get; set; }
    public DateTime? StaffConfirmedAtLocal { get; set; }
    public int? StaffDecisionVersion { get; set; }

    public string? HrDecision { get; set; }
    public string? HrComments { get; set; }
    public DateTime? HrDecidedAtLocal { get; set; }
    public string? HrDecidedByName { get; set; }

    public int? ActiveCustodyId { get; set; }
    public List<AssetAcknowledgementHistoryDto> AcknowledgementHistory { get; set; } = [];
    public List<AssetHrDecisionHistoryDto> HrDecisionHistory { get; set; } = [];
}

public sealed record AssetAcknowledgementHistoryDto(
    int AcknowledgementId,
    int ItemVersion,
    string Decision,
    string? RejectionReasonCode,
    string? Remarks,
    string ConfirmedByName,
    DateTime ConfirmedAtLocal);

public sealed record AssetHrDecisionHistoryDto(
    int HrDecisionId,
    int ItemVersion,
    string Decision,
    string? Comments,
    string DecidedByName,
    DateTime DecidedAtLocal);

public sealed class AssetIssuanceDraftInput
{
    public int AssetIssuanceId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int? StructureId { get; set; }
    public string Reason { get; set; } = "";
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.AddHours(5).Date;
    public bool ReserveImmediately { get; set; }
    public bool VerifyHandoverImmediately { get; set; }
    public string? HandoverNotes { get; set; }
    public List<AssetIssuanceLineDraftInput> Lines { get; set; } = [];
}

public sealed class AssetIssuanceLineDraftInput
{
    public int AssetIssuanceItemId { get; set; }
    public int AssetItemId { get; set; }
    public int ConditionAtIssueId { get; set; }
    public string? Accessories { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string? HandoverNotes { get; set; }
}

public sealed class AssetHandoverInput
{
    public int AssetIssuanceId { get; set; }
    public List<AssetHandoverLineInput> Lines { get; set; } = [];
}

public sealed class AssetHandoverLineInput
{
    public int AssetIssuanceItemId { get; set; }
    public int ConditionAtIssueId { get; set; }
    public string? Accessories { get; set; }
    public string? HandoverNotes { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
}

public sealed class AssetReplaceLineInput
{
    public int AssetIssuanceId { get; set; }
    public int RejectedOrReturnedIssuanceItemId { get; set; }
    public int ReplacementAssetItemId { get; set; }
    public int ConditionAtIssueId { get; set; }
    public string? Accessories { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public bool VerifyHandoverImmediately { get; set; } = true;
    public string? HandoverNotes { get; set; }
}

public sealed class MyAssetsPortalData
{
    public int IndividualId { get; set; }
    public string EmployeeNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string PositionTitle { get; set; } = "";
    public string OrganisationName { get; set; } = "";

    public List<AssetIssuanceDetailDto> PendingIssuances { get; set; } = [];
    public List<AssetCustodyRowDto> ActiveCustodies { get; set; } = [];
    public List<AssetCustodyRowDto> CustodyHistory { get; set; } = [];
    public List<AssetReturnDetailDto> Returns { get; set; } = [];
    public List<AssetDocumentSummaryDto> ApprovedDocuments { get; set; } = [];
}

public sealed class StaffAssetDecisionBatchInput
{
    public int AssetIssuanceId { get; set; }
    public bool ExplicitStatementConfirmed { get; set; }
    public List<StaffAssetLineDecisionInput> Decisions { get; set; } = [];
}

public sealed class StaffAssetLineDecisionInput
{
    public int AssetIssuanceItemId { get; set; }
    public int ExpectedVersion { get; set; } = 1;
    public string Decision { get; set; } = AssetStaffDecisions.Accepted;
    public string? RejectionReasonCode { get; set; }
    public string? Remarks { get; set; }
}

public sealed class AssetHrReviewBatchInput
{
    public int AssetIssuanceId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public List<AssetHrLineDecisionInput> Decisions { get; set; } = [];
}

public sealed class AssetHrLineDecisionInput
{
    public int AssetIssuanceItemId { get; set; }
    public int ExpectedVersion { get; set; } = 1;
    public string Decision { get; set; } = AssetHrDecisionKinds.HRApproved;
    public string? Comments { get; set; }
}

public sealed class AssetCustodyRowDto
{
    public int AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string CategoryName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string ConditionAtIssueName { get; set; } = "";
    public string? Accessories { get; set; }
    public int IndividualId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public int JobId { get; set; }
    public string PositionTitle { get; set; } = "";
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public int? StructureId { get; set; }
    public string DepartmentName { get; set; } = "";
    public int IssuanceId { get; set; }
    public string IssuanceReference { get; set; } = "";
    public int IssuanceItemId { get; set; }
    public DateTime CustodyFromLocal { get; set; }
    public DateTime? CustodyToLocal { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public bool IsOverdueReturn { get; set; }
    public string Status { get; set; } = AssetCustodyStatuses.Active;
    public string? ClosedReason { get; set; }
}

public sealed class AssetReturnCreateInput
{
    public int OrganisationId { get; set; }
    public int? IndividualId { get; set; }
    public int? StaffTransferId { get; set; }
    public string ReturnType { get; set; } = AssetReturnTypes.StandardReturn;
    public string? Notes { get; set; }
    public List<int> AssetCustodyIds { get; set; } = [];
}

public sealed class AssetReturnInspectInput
{
    public int AssetReturnId { get; set; }
    public string? Notes { get; set; }
    public List<AssetReturnItemInspectInput> Items { get; set; } = [];
}

public sealed class AssetReturnItemInspectInput
{
    public int AssetReturnItemId { get; set; }
    public int ConditionAtReturnId { get; set; }
    public string? AccessoryChecklist { get; set; }
    public string? MissingAccessories { get; set; }
    public string? DamageOrLossEvidence { get; set; }
    public string InspectionResult { get; set; } = AssetInspectionResults.GoodAvailable;
}

public sealed class AssetReturnDetailDto
{
    public int AssetReturnId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public int? JobId { get; set; }
    public int? StaffTransferId { get; set; }
    public string ReturnType { get; set; } = AssetReturnTypes.StandardReturn;
    public string RequestedByName { get; set; } = "";
    public string? ReturnedToName { get; set; }
    public DateTime ReturnDateLocal { get; set; }
    public DateTime? CompletedAtLocal { get; set; }
    public string Status { get; set; } = AssetReturnStatuses.ReturnPending;
    public string? Notes { get; set; }
    public List<AssetReturnItemDto> Items { get; set; } = [];
}

public sealed class AssetReturnItemDto
{
    public int AssetReturnItemId { get; set; }
    public int AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string CategoryName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int? ConditionAtReturnId { get; set; }
    public string? ConditionAtReturnName { get; set; }
    public string? AccessoryChecklist { get; set; }
    public string? MissingAccessories { get; set; }
    public string? DamageOrLossEvidence { get; set; }
    public string InspectionResult { get; set; } = AssetInspectionResults.Pending;
    public string? PostReturnDisposition { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime? ReceivedAtLocal { get; set; }
    public string Status { get; set; } = AssetReturnStatuses.ReturnPending;
}

public sealed class AssetTransferDispositionInput
{
    public int? StaffTransferId { get; set; }
    public int AssetCustodyId { get; set; }
    public int? ToJobId { get; set; }
    public int ToOrganisationId { get; set; }
    public int? ToStructureId { get; set; }
    public string Decision { get; set; } = AssetTransferDecisions.Retain;
    public string? Remarks { get; set; }
    public bool ExecuteImmediately { get; set; } = true;
}

public sealed class AssetTransferDispositionDto
{
    public int AssetTransferDispositionId { get; set; }
    public int? StaffTransferId { get; set; }
    public int AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string ItemName { get; set; } = "";
    public int IndividualId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int FromOrganisationId { get; set; }
    public int ToOrganisationId { get; set; }
    public int? FromStructureId { get; set; }
    public string FromDepartmentName { get; set; } = "";
    public int? ToStructureId { get; set; }
    public string ToDepartmentName { get; set; } = "";
    public string Decision { get; set; } = AssetTransferDecisions.Retain;
    public string Status { get; set; } = "Pending";
    public string DecidedByName { get; set; } = "";
    public DateTime DecidedAtLocal { get; set; }
    public DateTime? ExecutedAtLocal { get; set; }
    public string? Remarks { get; set; }
}

public sealed record AssetDocumentSummaryDto(
    long AssetDocumentId,
    int AssetIssuanceId,
    string DocumentNumber,
    string DocumentVariant,
    int DocumentVersion,
    string Sha256Hash,
    string GeneratedByName,
    DateTime GeneratedAtLocal);

public sealed class AssetPrintableDocumentDto
{
    public long AssetDocumentId { get; set; }
    public int AssetIssuanceId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentVariant { get; set; } = AssetDocumentVariants.StandardHandoverRecord;
    public int DocumentVersion { get; set; } = 1;
    public string Sha256Hash { get; set; } = "";
    public DateTime GeneratedAtLocal { get; set; }
    public string GeneratedByName { get; set; } = "";

    public string OrganisationName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string IssuanceReference { get; set; } = "";
    public string IssuanceStatus { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public string EmployeeName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public string PositionTitle { get; set; } = "";
    public string Reason { get; set; } = "";
    public string CustodyTermsText { get; set; } = "";
    public string AcknowledgementDisclaimerText { get; set; } = "";

    public List<AssetPrintableLineDto> Lines { get; set; } = [];
}

public sealed class AssetPrintableLineDto
{
    public int LineNumber { get; set; }
    public string CategoryName { get; set; } = "";
    public string AssetCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string BrandModel { get; set; } = "";
    public string SerialNumberDisplay { get; set; } = "";
    public string ConditionName { get; set; } = "";
    public string Accessories { get; set; } = "";
    public DateTime? HandedOverAtLocal { get; set; }
    public string HandedOverByName { get; set; } = "";
    public string StaffDecision { get; set; } = "";
    public string StaffRemarks { get; set; } = "";
    public DateTime? StaffConfirmedAtLocal { get; set; }
    public string HrDecision { get; set; } = "";
    public string HrOfficerName { get; set; } = "";
    public DateTime? HrDecidedAtLocal { get; set; }
    public string CustodyStatus { get; set; } = "";
    public int ItemVersion { get; set; } = 1;
}

public sealed record AssetEventDto(
    long AssetEventId,
    int? AssetItemId,
    string? AssetCode,
    string RelatedEntityType,
    int RelatedEntityId,
    string EventType,
    string? FromStatus,
    string? ToStatus,
    string ActorName,
    DateTime AtLocal,
    string? MetadataJson);
