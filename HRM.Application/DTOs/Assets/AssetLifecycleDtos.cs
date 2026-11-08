using HRM.DTOs.Organisation;
using HRM.Enums;

namespace HRM.DTOs.Assets;

// --- Service Requests ---

public sealed class AssetServiceRequestCreateInput
{
    public int? RequestId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int? StructureId { get; set; }
    public int? StaffTransferId { get; set; }
    public string RequestType { get; set; } = AssetServiceRequestTypes.FaultCorrective;
    public string Priority { get; set; } = "Standard";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<AssetServiceRequestItemInput> Items { get; set; } = [];
}

public sealed class AssetServiceRequestItemInput
{
    public int AssetItemId { get; set; }
    public int? AssetCustodyId { get; set; }
    public string ItemRequestType { get; set; } = AssetServiceRequestTypes.FaultCorrective;
    public string ReportedFaultOrRequirement { get; set; } = "";
    public string? TargetComponentCategory { get; set; }
    public string? TargetSpecification { get; set; }
}

public sealed class AssetServiceRequestDetailDto
{
    public int AssetServiceRequestId { get; set; }
    public int? RequestId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int IndividualId { get; set; }
    public string StaffName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public int? StructureId { get; set; }
    public string? DepartmentName { get; set; }
    public int? StaffTransferId { get; set; }
    public string RequestType { get; set; } = "";
    public string Priority { get; set; } = "Standard";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public int RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = "";
    public int? AssignedTechnicianUserId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResolutionSummary { get; set; }

    public List<AssetServiceRequestItemDto> Items { get; set; } = [];
    public List<AssetMaintenanceJobSummaryDto> MaintenanceJobs { get; set; } = [];
    public List<AssetReplacementLinkDto> ReplacementLinks { get; set; } = [];
    public List<AssetTemporaryHandoverDto> TemporaryHandovers { get; set; } = [];
    public List<AssetInspectionDto> Inspections { get; set; } = [];
    public List<AssetServiceDocumentSummaryDto> Documents { get; set; } = [];
}

public sealed class AssetServiceRequestSummaryDto
{
    public int AssetServiceRequestId { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public string RequestType { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string StaffName { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; }
    public int ItemsCount { get; set; }
}

public sealed class AssetServiceRequestItemDto
{
    public int AssetServiceRequestItemId { get; set; }
    public int AssetServiceRequestId { get; set; }
    public int? AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string AssetSummary { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string ItemRequestType { get; set; } = "";
    public string ReportedFaultOrRequirement { get; set; } = "";
    public string? TargetComponentCategory { get; set; }
    public string? TargetSpecification { get; set; }
    public string Status { get; set; } = "";
    public bool StaffAcknowledged { get; set; }
    public string? StaffDecision { get; set; }
    public string? StaffRemarks { get; set; }
    public DateTime? StaffConfirmedAtUtc { get; set; }
}

// --- Maintenance Jobs & Actions ---

public sealed class AssetTechnicianAssignInput
{
    public int AssetServiceRequestId { get; set; }
    public int? AssetServiceRequestItemId { get; set; }
    public int AssetItemId { get; set; }
    public int TechnicianUserId { get; set; }
    public string JobType { get; set; } = AssetMaintenanceJobTypes.Corrective;
    public string Priority { get; set; } = "Standard";
    public string? InitialDiagnosis { get; set; }
}

public sealed class AssetMaintenanceActionInput
{
    public int AssetMaintenanceJobId { get; set; }
    public string ActionType { get; set; } = "Diagnosis";
    public int DurationMinutes { get; set; }
    public string Remarks { get; set; } = "";
    public string? MetadataJson { get; set; }
}

public sealed class AssetMaintenanceJobCompleteInput
{
    public int AssetMaintenanceJobId { get; set; }
    public string DiagnosticSummary { get; set; } = "";
    public string WorkPerformedSummary { get; set; } = "";
    public string? HardwareTestSummary { get; set; }
    public bool MemTestPassed { get; set; }
    public bool SmartCheckPassed { get; set; }
    public bool ThermalBenchmarkPassed { get; set; }
    public bool PortsCheckPassed { get; set; }
    public string? DataSanitizationMethod { get; set; }
    public bool DataSanitizationVerified { get; set; }
    public string? VendorWarrantyClaimNumber { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LabourCost { get; set; }
    public string? ResolutionSummary { get; set; }
}

public sealed class AssetMaintenanceJobSummaryDto
{
    public int AssetMaintenanceJobId { get; set; }
    public string WorkOrderNumber { get; set; } = "";
    public int? AssetServiceRequestId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string JobType { get; set; } = "";
    public string Priority { get; set; } = "";
    public int AssignedTechnicianUserId { get; set; }
    public string TechnicianName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public decimal TotalCost { get; set; }
    public bool DataSanitizationVerified { get; set; }
    public List<AssetMaintenanceActionDto> Actions { get; set; } = [];
}

public sealed class AssetMaintenanceActionDto
{
    public long AssetMaintenanceActionId { get; set; }
    public int AssetMaintenanceJobId { get; set; }
    public string ActionType { get; set; } = "";
    public int TechnicianUserId { get; set; }
    public string TechnicianName { get; set; } = "";
    public DateTime ActionAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string Remarks { get; set; } = "";
}

// --- Hardware Upgrades & Component History ---

public sealed class AssetHardwareUpgradeInput
{
    public int AssetItemId { get; set; }
    public int? AssetMaintenanceJobId { get; set; }
    public int? AssetServiceRequestId { get; set; }
    public string ComponentType { get; set; } = AssetComponentTypes.RAM;
    public string? SlotOrPosition { get; set; }
    public int? RemovedAssetComponentId { get; set; }
    public string? RemovedPartDisposition { get; set; } = "RetainedSpare";
    public string NewComponentBrand { get; set; } = "";
    public string NewComponentModel { get; set; } = "";
    public string? NewComponentSerialNumber { get; set; }
    public string? NewComponentPartNumber { get; set; }
    public string NewComponentSpecification { get; set; } = "";
    public string? NewCapacityOrRating { get; set; }
    public decimal? NewComponentCost { get; set; }
    public string UpgradeRemarks { get; set; } = "";
}

public sealed class AssetComponentDto
{
    public int AssetComponentId { get; set; }
    public int? AssetItemId { get; set; }
    public string ComponentType { get; set; } = "";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string Specification { get; set; } = "";
    public string? CapacityOrRating { get; set; }
    public string? SlotOrPortLocation { get; set; }
    public string Status { get; set; } = "";
    public decimal? Cost { get; set; }
    public DateTime? InstalledAtUtc { get; set; }
    public DateTime? RemovedAtUtc { get; set; }
}

public sealed class AssetComponentTransactionDto
{
    public long AssetComponentTransactionId { get; set; }
    public int AssetComponentId { get; set; }
    public string ComponentSummary { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public string TechnicianName { get; set; } = "";
    public DateTime TransactedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class AssetConfigurationTimelineDto
{
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string CurrentRamSummary { get; set; } = "";
    public string CurrentStorageSummary { get; set; } = "";
    public string? CurrentProcessorSpec { get; set; }
    public string? CurrentGraphicsSpec { get; set; }
    public int? CurrentBatteryHealth { get; set; }
    public string? CurrentOsVersion { get; set; }
    public List<AssetComponentDto> ActiveComponents { get; set; } = [];
    public List<AssetComponentTransactionDto> ComponentHistory { get; set; } = [];
    public List<AssetConfigurationSnapshotDto> Snapshots { get; set; } = [];
}

public sealed class AssetConfigurationSnapshotDto
{
    public long AssetConfigurationSnapshotId { get; set; }
    public string TriggerEvent { get; set; } = "";
    public string? ProcessorSpec { get; set; }
    public string RamSummary { get; set; } = "";
    public string StorageSummary { get; set; } = "";
    public string? GraphicsSpec { get; set; }
    public int? BatteryHealthPercentage { get; set; }
    public string? OsVersion { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public string CapturedByName { get; set; } = "";
}

// --- Spare Parts Stock Inventory ---

public sealed class AssetServicePartDto
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
    public int ReorderLevel { get; set; }
    public string? LocationBin { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AssetServicePartSaveInput
{
    public int? AssetServicePartId { get; set; }
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
}

// --- Equipment Replacement Links ---

public sealed class AssetReplacementLinkInput
{
    public int? AssetServiceRequestId { get; set; }
    public int OriginalAssetItemId { get; set; }
    public int? OriginalAssetCustodyId { get; set; }
    public int ReplacementAssetItemId { get; set; }
    public string ReplacementType { get; set; } = AssetReplacementTypes.CompleteDeviceReplacement;
    public string ReplacementReason { get; set; } = AssetReplacementReasons.BeyondEconomicalRepair;
    public string OriginalAssetDisposition { get; set; } = "PendingReturn";
    public string? StaffRemarks { get; set; }
}

public sealed class AssetReplacementLinkDto
{
    public int AssetReplacementLinkId { get; set; }
    public int? AssetServiceRequestId { get; set; }
    public int OriginalAssetItemId { get; set; }
    public string OriginalAssetCode { get; set; } = "";
    public string OriginalAssetSummary { get; set; } = "";
    public int? OriginalAssetCustodyId { get; set; }
    public int ReplacementAssetItemId { get; set; }
    public string ReplacementAssetCode { get; set; } = "";
    public string ReplacementAssetSummary { get; set; } = "";
    public int? ReplacementAssetCustodyId { get; set; }
    public string ReplacementType { get; set; } = "";
    public string ReplacementReason { get; set; } = "";
    public string OriginalAssetDisposition { get; set; } = "";
    public bool StaffAcknowledged { get; set; }
    public string? StaffDecision { get; set; }
    public string? StaffRemarks { get; set; }
    public DateTime? StaffConfirmedAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string RecordedByName { get; set; } = "";
}

// --- Temporary Loaner Equipment ---

public sealed class AssetLoanerIssueInput
{
    public int? AssetServiceRequestId { get; set; }
    public int PrimaryAssetItemId { get; set; }
    public int LoanerAssetItemId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public int ConditionOutId { get; set; }
    public string? Notes { get; set; }
}

public sealed class AssetLoanerReturnInput
{
    public int AssetTemporaryHandoverId { get; set; }
    public int ConditionInId { get; set; }
    public string? Notes { get; set; }
}

public sealed class AssetTemporaryHandoverDto
{
    public int AssetTemporaryHandoverId { get; set; }
    public int? AssetServiceRequestId { get; set; }
    public int PrimaryAssetItemId { get; set; }
    public string PrimaryAssetCode { get; set; } = "";
    public string PrimaryAssetSummary { get; set; } = "";
    public int LoanerAssetItemId { get; set; }
    public string LoanerAssetCode { get; set; } = "";
    public string LoanerAssetSummary { get; set; } = "";
    public int IndividualId { get; set; }
    public string StaffName { get; set; } = "";
    public DateTime HandedOverAtUtc { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnedAtUtc { get; set; }
    public string ConditionOutName { get; set; } = "";
    public string? ConditionInName { get; set; }
    public string Status { get; set; } = "";
    public bool StaffAcknowledgementConfirmed { get; set; }
    public DateTime? StaffConfirmedAtUtc { get; set; }
    public string? Notes { get; set; }
}

// --- Inspections ---

public sealed class AssetInspectionInput
{
    public int AssetItemId { get; set; }
    public int? AssetCustodyId { get; set; }
    public int? AssetServiceRequestId { get; set; }
    public int? AssetReturnItemId { get; set; }
    public string InspectionStage { get; set; } = AssetInspectionStages.PreServiceTriage;
    public int OverallConditionId { get; set; }
    public string? PhysicalDamageReport { get; set; }
    public string? FunctionalTestSummary { get; set; }
    public string? AccessoriesChecklist { get; set; }
    public bool DataSanitizationVerified { get; set; }
    public string InspectorDecision { get; set; } = AssetInspectorDecisions.Passed;
    public string? Notes { get; set; }
}

public sealed class AssetInspectionDto
{
    public int AssetInspectionId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string InspectionStage { get; set; } = "";
    public string InspectorName { get; set; } = "";
    public DateTime InspectedAtUtc { get; set; }
    public string OverallConditionName { get; set; } = "";
    public string? PhysicalDamageReport { get; set; }
    public string? FunctionalTestSummary { get; set; }
    public string? AccessoriesChecklist { get; set; }
    public bool DataSanitizationVerified { get; set; }
    public string InspectorDecision { get; set; } = "";
    public string? Notes { get; set; }
}

// --- Post-Handover Staff Decision ---

public sealed class AssetServiceStaffAcknowledgeInput
{
    public int AssetServiceRequestItemId { get; set; }
    public string StaffDecision { get; set; } = "Accepted"; // Accepted, Rejected
    public string? StaffRemarks { get; set; }
}

// --- Staff Asset Clearance Check ---

public sealed class AssetClearanceCheckDto
{
    public int IndividualId { get; set; }
    public int? JobId { get; set; }
    public string StaffName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public bool IsEligibleForClearance { get; set; }
    public int ActiveCustodiesCount { get; set; }
    public int ActiveLoanersCount { get; set; }
    public int OpenServiceRequestsCount { get; set; }
    public List<AssetCustodyRowDto> ActiveAssignedCustodies { get; set; } = [];
    public List<AssetTemporaryHandoverDto> ActiveLoaners { get; set; } = [];
    public List<AssetServiceRequestSummaryDto> OpenServiceRequests { get; set; } = [];
    public string ClearanceSummary { get; set; } = "";
    public string? BlockingReason { get; set; }
}

// --- Printable Lifecycle Documents ---

public sealed class AssetServiceDocumentSummaryDto
{
    public long AssetServiceDocumentId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public int DocumentVersion { get; set; }
    public string Sha256Hash { get; set; } = "";
    public DateTime GeneratedAtUtc { get; set; }
    public string GeneratedByName { get; set; } = "";
}

public sealed class AssetServicePrintableDocumentDto
{
    public long AssetServiceDocumentId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public int DocumentVersion { get; set; }
    public string Title { get; set; } = "";
    public string OrganisationName { get; set; } = "";
    public string StaffName { get; set; } = "";
    public string StaffNumber { get; set; } = "";
    public string AssetCode { get; set; } = "";
    public string AssetDescription { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public DateTime GeneratedAtUtc { get; set; }
    public string GeneratedByName { get; set; } = "";
    public string Sha256Hash { get; set; } = "";
    public string SummaryDetails { get; set; } = "";
    public Dictionary<string, string> KeyAttributes { get; set; } = [];
    public List<string> ChecklistItems { get; set; } = [];
    public string VerificationQrUrl { get; set; } = "";
}
