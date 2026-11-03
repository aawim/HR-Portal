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
    }
}
