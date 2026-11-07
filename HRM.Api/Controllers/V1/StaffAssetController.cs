// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-asset")]
public sealed class StaffAssetController(global::HRM.Services.Contracts.StaffAssetServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load-dashboard")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.AssetDashboardData>> LoadDashboard([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.String? statusFilter = null)
    {
        return Ok(await service.LoadDashboardAsync(organisationId, statusFilter));
    }

    [HttpPost("save-category")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveCategory([FromBody] StaffAssetSaveCategoryRequest request)
    {
        return Ok(await service.SaveCategoryAsync(request.input));
    }

    public sealed class StaffAssetSaveCategoryRequest
    {
        public global::HRM.DTOs.Assets.AssetCategorySaveInput input { get; set; } = default!;
    }

    [HttpPost("save-asset-item")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveAssetItem([FromBody] StaffAssetSaveAssetItemRequest request)
    {
        return Ok(await service.SaveAssetItemAsync(request.input));
    }

    public sealed class StaffAssetSaveAssetItemRequest
    {
        public global::HRM.DTOs.Assets.AssetItemSaveInput input { get; set; } = default!;
    }

    [HttpGet("load-issuance-detail")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.AssetIssuanceDetailDto>> LoadIssuanceDetail([FromQuery] global::System.Int32 assetIssuanceId)
    {
        return Ok(await service.LoadIssuanceDetailAsync(assetIssuanceId));
    }

    [HttpPost("save-issuance-draft")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveIssuanceDraft([FromBody] StaffAssetSaveIssuanceDraftRequest request)
    {
        return Ok(await service.SaveIssuanceDraftAsync(request.input));
    }

    public sealed class StaffAssetSaveIssuanceDraftRequest
    {
        public global::HRM.DTOs.Assets.AssetIssuanceDraftInput input { get; set; } = default!;
    }

    [HttpPost("reserve-issuance-lines")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ReserveIssuanceLines([FromBody] StaffAssetReserveIssuanceLinesRequest request)
    {
        return Ok(await service.ReserveIssuanceLinesAsync(request.assetIssuanceId));
    }

    public sealed class StaffAssetReserveIssuanceLinesRequest
    {
        public global::System.Int32 assetIssuanceId { get; set; }
    }

    [HttpPost("verify-handover")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> VerifyHandover([FromBody] StaffAssetVerifyHandoverRequest request)
    {
        return Ok(await service.VerifyHandoverAsync(request.input));
    }

    public sealed class StaffAssetVerifyHandoverRequest
    {
        public global::HRM.DTOs.Assets.AssetHandoverInput input { get; set; } = default!;
    }

    [HttpGet("cancel-issuance")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CancelIssuance([FromQuery] global::System.Int32 assetIssuanceId, [FromQuery] global::System.String reason)
    {
        return Ok(await service.CancelIssuanceAsync(assetIssuanceId, reason));
    }

    [HttpGet("load-my-assets-portal")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.MyAssetsPortalData>> LoadMyAssetsPortal()
    {
        return Ok(await service.LoadMyAssetsPortalAsync());
    }

    [HttpPost("submit-staff-decisions")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitStaffDecisions([FromBody] StaffAssetSubmitStaffDecisionsRequest request)
    {
        return Ok(await service.SubmitStaffDecisionsAsync(request.input));
    }

    public sealed class StaffAssetSubmitStaffDecisionsRequest
    {
        public global::HRM.DTOs.Assets.StaffAssetDecisionBatchInput input { get; set; } = default!;
    }

    [HttpPost("submit-hr-review")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitHrReview([FromBody] StaffAssetSubmitHrReviewRequest request)
    {
        return Ok(await service.SubmitHrReviewAsync(request.input));
    }

    public sealed class StaffAssetSubmitHrReviewRequest
    {
        public global::HRM.DTOs.Assets.AssetHrReviewBatchInput input { get; set; } = default!;
    }

    [HttpPost("replace-issuance-line")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ReplaceIssuanceLine([FromBody] StaffAssetReplaceIssuanceLineRequest request)
    {
        return Ok(await service.ReplaceIssuanceLineAsync(request.input));
    }

    public sealed class StaffAssetReplaceIssuanceLineRequest
    {
        public global::HRM.DTOs.Assets.AssetReplaceLineInput input { get; set; } = default!;
    }

    [HttpPost("create-return")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreateReturn([FromBody] StaffAssetCreateReturnRequest request)
    {
        return Ok(await service.CreateReturnAsync(request.input));
    }

    public sealed class StaffAssetCreateReturnRequest
    {
        public global::HRM.DTOs.Assets.AssetReturnCreateInput input { get; set; } = default!;
    }

    [HttpPost("inspect-return")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> InspectReturn([FromBody] StaffAssetInspectReturnRequest request)
    {
        return Ok(await service.InspectReturnAsync(request.input));
    }

    public sealed class StaffAssetInspectReturnRequest
    {
        public global::HRM.DTOs.Assets.AssetReturnInspectInput input { get; set; } = default!;
    }

    [HttpPost("record-transfer-disposition")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RecordTransferDisposition([FromBody] StaffAssetRecordTransferDispositionRequest request)
    {
        return Ok(await service.RecordTransferDispositionAsync(request.input));
    }

    public sealed class StaffAssetRecordTransferDispositionRequest
    {
        public global::HRM.DTOs.Assets.AssetTransferDispositionInput input { get; set; } = default!;
    }

    [HttpGet("get-printable-document")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.AssetPrintableDocumentDto>> GetPrintableDocument([FromQuery] global::System.Int32 assetIssuanceId, [FromQuery] global::System.String variant = "StandardHandoverRecord")
    {
        return Ok(await service.GetPrintableDocumentAsync(assetIssuanceId, variant));
    }

    [HttpGet("load-service-request-detail")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.AssetServiceRequestDetailDto>> LoadServiceRequestDetail([FromQuery] global::System.Int32 assetServiceRequestId)
    {
        return Ok(await service.LoadServiceRequestDetailAsync(assetServiceRequestId));
    }

    [HttpPost("create-service-request")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreateServiceRequest([FromBody] StaffAssetCreateServiceRequestRequest request)
    {
        return Ok(await service.CreateServiceRequestAsync(request.input));
    }

    public sealed class StaffAssetCreateServiceRequestRequest
    {
        public global::HRM.DTOs.Assets.AssetServiceRequestCreateInput input { get; set; } = default!;
    }

    [HttpPost("assign-technician")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AssignTechnician([FromBody] StaffAssetAssignTechnicianRequest request)
    {
        return Ok(await service.AssignTechnicianAsync(request.input));
    }

    public sealed class StaffAssetAssignTechnicianRequest
    {
        public global::HRM.DTOs.Assets.AssetTechnicianAssignInput input { get; set; } = default!;
    }

    [HttpPost("record-maintenance-action")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RecordMaintenanceAction([FromBody] StaffAssetRecordMaintenanceActionRequest request)
    {
        return Ok(await service.RecordMaintenanceActionAsync(request.input));
    }

    public sealed class StaffAssetRecordMaintenanceActionRequest
    {
        public global::HRM.DTOs.Assets.AssetMaintenanceActionInput input { get; set; } = default!;
    }

    [HttpPost("complete-maintenance-job")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CompleteMaintenanceJob([FromBody] StaffAssetCompleteMaintenanceJobRequest request)
    {
        return Ok(await service.CompleteMaintenanceJobAsync(request.input));
    }

    public sealed class StaffAssetCompleteMaintenanceJobRequest
    {
        public global::HRM.DTOs.Assets.AssetMaintenanceJobCompleteInput input { get; set; } = default!;
    }

    [HttpPost("perform-hardware-upgrade")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> PerformHardwareUpgrade([FromBody] StaffAssetPerformHardwareUpgradeRequest request)
    {
        return Ok(await service.PerformHardwareUpgradeAsync(request.input));
    }

    public sealed class StaffAssetPerformHardwareUpgradeRequest
    {
        public global::HRM.DTOs.Assets.AssetHardwareUpgradeInput input { get; set; } = default!;
    }

    [HttpPost("query/issue-loaner-asset")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> IssueLoanerAsset([FromBody] StaffAssetIssueLoanerAssetRequest request)
    {
        return Ok(await service.IssueLoanerAssetAsync(request.input));
    }

    public sealed class StaffAssetIssueLoanerAssetRequest
    {
        public global::HRM.DTOs.Assets.AssetLoanerIssueInput input { get; set; } = default!;
    }

    [HttpPost("return-loaner-asset")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ReturnLoanerAsset([FromBody] StaffAssetReturnLoanerAssetRequest request)
    {
        return Ok(await service.ReturnLoanerAssetAsync(request.input));
    }

    public sealed class StaffAssetReturnLoanerAssetRequest
    {
        public global::HRM.DTOs.Assets.AssetLoanerReturnInput input { get; set; } = default!;
    }

    [HttpPost("record-replacement-link")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RecordReplacementLink([FromBody] StaffAssetRecordReplacementLinkRequest request)
    {
        return Ok(await service.RecordReplacementLinkAsync(request.input));
    }

    public sealed class StaffAssetRecordReplacementLinkRequest
    {
        public global::HRM.DTOs.Assets.AssetReplacementLinkInput input { get; set; } = default!;
    }

    [HttpPost("acknowledge-service-item")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AcknowledgeServiceItem([FromBody] StaffAssetAcknowledgeServiceItemRequest request)
    {
        return Ok(await service.AcknowledgeServiceItemAsync(request.input));
    }

    public sealed class StaffAssetAcknowledgeServiceItemRequest
    {
        public global::HRM.DTOs.Assets.AssetServiceStaffAcknowledgeInput input { get; set; } = default!;
    }

    [HttpPost("record-inspection")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RecordInspection([FromBody] StaffAssetRecordInspectionRequest request)
    {
        return Ok(await service.RecordInspectionAsync(request.input));
    }

    public sealed class StaffAssetRecordInspectionRequest
    {
        public global::HRM.DTOs.Assets.AssetInspectionInput input { get; set; } = default!;
    }

    [HttpGet("get-asset-configuration-history")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.AssetConfigurationTimelineDto>> GetAssetConfigurationHistory([FromQuery] global::System.Int32 assetItemId)
    {
        return Ok(await service.GetAssetConfigurationHistoryAsync(assetItemId));
    }

    [HttpPost("check-staff-asset-clearance")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.AssetClearanceCheckDto>> CheckStaffAssetClearance([FromBody] StaffAssetCheckStaffAssetClearanceRequest request)
    {
        return Ok(await service.CheckStaffAssetClearanceAsync(request.individualId, request.staffTransferId));
    }

    public sealed class StaffAssetCheckStaffAssetClearanceRequest
    {
        public global::System.Int32 individualId { get; set; }
        public global::System.Int32? staffTransferId { get; set; } = null;
    }

    [HttpGet("get-service-printable-document")]
    public async Task<ActionResult<global::HRM.DTOs.Assets.AssetServicePrintableDocumentDto>> GetServicePrintableDocument([FromQuery] global::System.Int64 documentIdOrRequestId, [FromQuery] global::System.String documentType = "ServiceReport")
    {
        return Ok(await service.GetServicePrintableDocumentAsync(documentIdOrRequestId, documentType));
    }

    [HttpPost("save-service-part")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveServicePart([FromBody] StaffAssetSaveServicePartRequest request)
    {
        return Ok(await service.SaveServicePartAsync(request.input));
    }

    public sealed class StaffAssetSaveServicePartRequest
    {
        public global::HRM.DTOs.Assets.AssetServicePartSaveInput input { get; set; } = default!;
    }

}
