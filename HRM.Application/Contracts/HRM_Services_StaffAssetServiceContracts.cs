using HRM.DTOs;
using HRM.DTOs.Assets;
using HRM.Enums;

namespace HRM.Services.Contracts;

public abstract class StaffAssetServiceContracts
{
    public const string RequestTypeName = "Staff Asset Issuance Request";
    public const string AdminPageUrl = "/admin/hr/items/asset-custody";
    public const string OnlinePortalUrl = "/online/my-assets";

    public interface IOperations
    {
        Task<AssetDashboardData> LoadDashboardAsync(int? organisationId = null, string? statusFilter = null);
        Task<ServiceResult> SaveCategoryAsync(AssetCategorySaveInput input);
        Task<ServiceResult> SaveAssetItemAsync(AssetItemSaveInput input);
        Task<AssetIssuanceDetailDto> LoadIssuanceDetailAsync(int assetIssuanceId);
        Task<ServiceResult> SaveIssuanceDraftAsync(AssetIssuanceDraftInput input);
        Task<ServiceResult> ReserveIssuanceLinesAsync(int assetIssuanceId);
        Task<ServiceResult> VerifyHandoverAsync(AssetHandoverInput input);
        Task<ServiceResult> CancelIssuanceAsync(int assetIssuanceId, string reason);
        Task<MyAssetsPortalData> LoadMyAssetsPortalAsync();
        Task<ServiceResult> SubmitStaffDecisionsAsync(StaffAssetDecisionBatchInput input);
        Task<ServiceResult> SubmitHrReviewAsync(AssetHrReviewBatchInput input);
        Task<ServiceResult> ReplaceIssuanceLineAsync(AssetReplaceLineInput input);
        Task<ServiceResult> CreateReturnAsync(AssetReturnCreateInput input);
        Task<ServiceResult> InspectReturnAsync(AssetReturnInspectInput input);
        Task<ServiceResult> RecordTransferDispositionAsync(AssetTransferDispositionInput input);
        Task<AssetPrintableDocumentDto> GetPrintableDocumentAsync(int assetIssuanceId, string variant = AssetDocumentVariants.StandardHandoverRecord);

        // --- Lifecycle, Maintenance, Upgrades, Replacement, Loaners & Clearance ---
        Task<AssetServiceRequestDetailDto> LoadServiceRequestDetailAsync(int assetServiceRequestId);
        Task<ServiceResult> CreateServiceRequestAsync(AssetServiceRequestCreateInput input);
        Task<ServiceResult> AssignTechnicianAsync(AssetTechnicianAssignInput input);
        Task<ServiceResult> RecordMaintenanceActionAsync(AssetMaintenanceActionInput input);
        Task<ServiceResult> CompleteMaintenanceJobAsync(AssetMaintenanceJobCompleteInput input);
        Task<ServiceResult> PerformHardwareUpgradeAsync(AssetHardwareUpgradeInput input);
        Task<ServiceResult> IssueLoanerAssetAsync(AssetLoanerIssueInput input);
        Task<ServiceResult> ReturnLoanerAssetAsync(AssetLoanerReturnInput input);
        Task<ServiceResult> RecordReplacementLinkAsync(AssetReplacementLinkInput input);
        Task<ServiceResult> AcknowledgeServiceItemAsync(AssetServiceStaffAcknowledgeInput input);
        Task<ServiceResult> RecordInspectionAsync(AssetInspectionInput input);
        Task<AssetConfigurationTimelineDto> GetAssetConfigurationHistoryAsync(int assetItemId);
        Task<AssetClearanceCheckDto> CheckStaffAssetClearanceAsync(int individualId, int? staffTransferId = null);
        Task<AssetServicePrintableDocumentDto> GetServicePrintableDocumentAsync(long documentIdOrRequestId, string documentType = "ServiceReport");
        Task<ServiceResult> SaveServicePartAsync(AssetServicePartSaveInput input);
    }
}
