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
}
