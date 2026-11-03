using HRM.Api.Infrastructure;
using HRM.DTOs;
using HRM.DTOs.Assets;
using HRM.Enums;
using HRM.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-asset")]
public sealed class StaffAssetController : HrmApiController
{
    private readonly StaffAssetServiceContracts.IOperations _service;

    public StaffAssetController(StaffAssetServiceContracts.IOperations service)
    {
        _service = service;
    }

    [HttpGet("load-dashboard")]
    public Task<AssetDashboardData> LoadDashboardAsync(
        [FromQuery] int? organisationId = null,
        [FromQuery] string? statusFilter = null)
        => _service.LoadDashboardAsync(organisationId, statusFilter);

    [HttpPost("save-category")]
    public Task<ServiceResult> SaveCategoryAsync([FromBody] SaveCategoryRequest request)
        => _service.SaveCategoryAsync(request.input);

    [HttpPost("save-asset-item")]
    public Task<ServiceResult> SaveAssetItemAsync([FromBody] SaveAssetItemRequest request)
        => _service.SaveAssetItemAsync(request.input);

    [HttpGet("load-issuance-detail")]
    public Task<AssetIssuanceDetailDto> LoadIssuanceDetailAsync([FromQuery] int assetIssuanceId)
        => _service.LoadIssuanceDetailAsync(assetIssuanceId);

    [HttpPost("save-issuance-draft")]
    public Task<ServiceResult> SaveIssuanceDraftAsync([FromBody] SaveIssuanceDraftRequest request)
        => _service.SaveIssuanceDraftAsync(request.input);

    [HttpPost("reserve-issuance-lines")]
    public Task<ServiceResult> ReserveIssuanceLinesAsync([FromBody] ReserveIssuanceLinesRequest request)
        => _service.ReserveIssuanceLinesAsync(request.assetIssuanceId);

    [HttpPost("verify-handover")]
    public Task<ServiceResult> VerifyHandoverAsync([FromBody] VerifyHandoverRequest request)
        => _service.VerifyHandoverAsync(request.input);

    [HttpPost("cancel-issuance")]
    public Task<ServiceResult> CancelIssuanceAsync([FromBody] CancelIssuanceRequest request)
        => _service.CancelIssuanceAsync(request.assetIssuanceId, request.reason);

    [HttpGet("load-my-assets-portal")]
    public Task<MyAssetsPortalData> LoadMyAssetsPortalAsync()
        => _service.LoadMyAssetsPortalAsync();

    [HttpPost("submit-staff-decisions")]
    public Task<ServiceResult> SubmitStaffDecisionsAsync([FromBody] SubmitStaffDecisionsRequest request)
        => _service.SubmitStaffDecisionsAsync(request.input);

    [HttpPost("submit-hr-review")]
    public Task<ServiceResult> SubmitHrReviewAsync([FromBody] SubmitHrReviewRequest request)
        => _service.SubmitHrReviewAsync(request.input);

    [HttpPost("replace-issuance-line")]
    public Task<ServiceResult> ReplaceIssuanceLineAsync([FromBody] ReplaceIssuanceLineRequest request)
        => _service.ReplaceIssuanceLineAsync(request.input);

    [HttpPost("create-return")]
    public Task<ServiceResult> CreateReturnAsync([FromBody] CreateReturnRequest request)
        => _service.CreateReturnAsync(request.input);

    [HttpPost("inspect-return")]
    public Task<ServiceResult> InspectReturnAsync([FromBody] InspectReturnRequest request)
        => _service.InspectReturnAsync(request.input);

    [HttpPost("record-transfer-disposition")]
    public Task<ServiceResult> RecordTransferDispositionAsync([FromBody] RecordTransferDispositionRequest request)
        => _service.RecordTransferDispositionAsync(request.input);

    [HttpGet("get-printable-document")]
    public Task<AssetPrintableDocumentDto> GetPrintableDocumentAsync(
        [FromQuery] int assetIssuanceId,
        [FromQuery] string variant = AssetDocumentVariants.StandardHandoverRecord)
        => _service.GetPrintableDocumentAsync(assetIssuanceId, variant);

    [HttpGet("load-service-request-detail")]
    public Task<AssetServiceRequestDetailDto> LoadServiceRequestDetailAsync([FromQuery] int assetServiceRequestId)
        => _service.LoadServiceRequestDetailAsync(assetServiceRequestId);

    [HttpPost("create-service-request")]
    public Task<ServiceResult> CreateServiceRequestAsync([FromBody] CreateServiceRequestRequest request)
        => _service.CreateServiceRequestAsync(request.input);

    [HttpPost("assign-technician")]
    public Task<ServiceResult> AssignTechnicianAsync([FromBody] AssignTechnicianRequest request)
        => _service.AssignTechnicianAsync(request.input);

    [HttpPost("record-maintenance-action")]
    public Task<ServiceResult> RecordMaintenanceActionAsync([FromBody] RecordMaintenanceActionRequest request)
        => _service.RecordMaintenanceActionAsync(request.input);

    [HttpPost("complete-maintenance-job")]
    public Task<ServiceResult> CompleteMaintenanceJobAsync([FromBody] CompleteMaintenanceJobRequest request)
        => _service.CompleteMaintenanceJobAsync(request.input);

    [HttpPost("perform-hardware-upgrade")]
    public Task<ServiceResult> PerformHardwareUpgradeAsync([FromBody] PerformHardwareUpgradeRequest request)
        => _service.PerformHardwareUpgradeAsync(request.input);

    [HttpPost("issue-loaner-asset")]
    public Task<ServiceResult> IssueLoanerAssetAsync([FromBody] IssueLoanerAssetRequest request)
        => _service.IssueLoanerAssetAsync(request.input);

    [HttpPost("return-loaner-asset")]
    public Task<ServiceResult> ReturnLoanerAssetAsync([FromBody] ReturnLoanerAssetRequest request)
        => _service.ReturnLoanerAssetAsync(request.input);

    [HttpPost("record-replacement-link")]
    public Task<ServiceResult> RecordReplacementLinkAsync([FromBody] RecordReplacementLinkRequest request)
        => _service.RecordReplacementLinkAsync(request.input);

    [HttpPost("acknowledge-service-item")]
    public Task<ServiceResult> AcknowledgeServiceItemAsync([FromBody] AcknowledgeServiceItemRequest request)
        => _service.AcknowledgeServiceItemAsync(request.input);

    [HttpPost("record-inspection")]
    public Task<ServiceResult> RecordInspectionAsync([FromBody] RecordInspectionRequest request)
        => _service.RecordInspectionAsync(request.input);

    [HttpGet("get-asset-configuration-history")]
    public Task<AssetConfigurationTimelineDto> GetAssetConfigurationHistoryAsync([FromQuery] int assetItemId)
        => _service.GetAssetConfigurationHistoryAsync(assetItemId);

    [HttpGet("check-staff-asset-clearance")]
    public Task<AssetClearanceCheckDto> CheckStaffAssetClearanceAsync([FromQuery] int individualId, [FromQuery] int? staffTransferId = null)
        => _service.CheckStaffAssetClearanceAsync(individualId, staffTransferId);

    [HttpGet("get-service-printable-document")]
    public Task<AssetServicePrintableDocumentDto> GetServicePrintableDocumentAsync([FromQuery] long documentIdOrRequestId, [FromQuery] string documentType = "ServiceReport")
        => _service.GetServicePrintableDocumentAsync(documentIdOrRequestId, documentType);

    [HttpPost("save-service-part")]
    public Task<ServiceResult> SaveServicePartAsync([FromBody] SaveServicePartRequest request)
        => _service.SaveServicePartAsync(request.input);

    public sealed class SaveCategoryRequest
    {
        public AssetCategorySaveInput input { get; set; } = new();
    }

    public sealed class SaveAssetItemRequest
    {
        public AssetItemSaveInput input { get; set; } = new();
    }

    public sealed class SaveIssuanceDraftRequest
    {
        public AssetIssuanceDraftInput input { get; set; } = new();
    }

    public sealed class ReserveIssuanceLinesRequest
    {
        public int assetIssuanceId { get; set; }
    }

    public sealed class VerifyHandoverRequest
    {
        public AssetHandoverInput input { get; set; } = new();
    }

    public sealed class CancelIssuanceRequest
    {
        public int assetIssuanceId { get; set; }
        public string reason { get; set; } = "";
    }

    public sealed class SubmitStaffDecisionsRequest
    {
        public StaffAssetDecisionBatchInput input { get; set; } = new();
    }

    public sealed class SubmitHrReviewRequest
    {
        public AssetHrReviewBatchInput input { get; set; } = new();
    }

    public sealed class ReplaceIssuanceLineRequest
    {
        public AssetReplaceLineInput input { get; set; } = new();
    }

    public sealed class CreateReturnRequest
    {
        public AssetReturnCreateInput input { get; set; } = new();
    }

    public sealed class InspectReturnRequest
    {
        public AssetReturnInspectInput input { get; set; } = new();
    }

    public sealed class RecordTransferDispositionRequest
    {
        public AssetTransferDispositionInput input { get; set; } = new();
    }

    public sealed class CreateServiceRequestRequest
    {
        public AssetServiceRequestCreateInput input { get; set; } = new();
    }

    public sealed class AssignTechnicianRequest
    {
        public AssetTechnicianAssignInput input { get; set; } = new();
    }

    public sealed class RecordMaintenanceActionRequest
    {
        public AssetMaintenanceActionInput input { get; set; } = new();
    }

    public sealed class CompleteMaintenanceJobRequest
    {
        public AssetMaintenanceJobCompleteInput input { get; set; } = new();
    }

    public sealed class PerformHardwareUpgradeRequest
    {
        public AssetHardwareUpgradeInput input { get; set; } = new();
    }

    public sealed class IssueLoanerAssetRequest
    {
        public AssetLoanerIssueInput input { get; set; } = new();
    }

    public sealed class ReturnLoanerAssetRequest
    {
        public AssetLoanerReturnInput input { get; set; } = new();
    }

    public sealed class RecordReplacementLinkRequest
    {
        public AssetReplacementLinkInput input { get; set; } = new();
    }

    public sealed class AcknowledgeServiceItemRequest
    {
        public AssetServiceStaffAcknowledgeInput input { get; set; } = new();
    }

    public sealed class RecordInspectionRequest
    {
        public AssetInspectionInput input { get; set; } = new();
    }

    public sealed class SaveServicePartRequest
    {
        public AssetServicePartSaveInput input { get; set; } = new();
    }
}
