// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-separation")]
public sealed class StaffSeparationController(global::HRM.Services.Contracts.StaffSeparationServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load-dashboard")]
    public async Task<ActionResult<global::HRM.DTOs.Separation.StaffSeparationDashboardData>> LoadDashboard([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.String? statusFilter = null, [FromQuery] global::System.String? search = null)
    {
        return Ok(await service.LoadDashboardAsync(organisationId, statusFilter, search));
    }

    [HttpGet("load-form-lookups")]
    public async Task<ActionResult<global::HRM.DTOs.Separation.StaffSeparationFormLookupsDto>> LoadFormLookups([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.LoadFormLookupsAsync(organisationId));
    }

    [HttpGet("load-detail")]
    public async Task<ActionResult<global::HRM.DTOs.Separation.StaffSeparationDetailDto>> LoadDetail([FromQuery] global::System.Int32? staffSeparationId = null, [FromQuery] global::System.Int32? individualId = null, [FromQuery] global::System.Int32? jobId = null)
    {
        return Ok(await service.LoadDetailAsync(staffSeparationId, individualId, jobId));
    }

    [HttpPost("initiate-separation")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> InitiateSeparation([FromBody] StaffSeparationInitiateSeparationRequest request)
    {
        return Ok(await service.InitiateSeparationAsync(request.input));
    }

    public sealed class StaffSeparationInitiateSeparationRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationInitiateInput input { get; set; } = default!;
    }

    [HttpPost("acknowledge-exit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AcknowledgeExit([FromBody] StaffSeparationAcknowledgeExitRequest request)
    {
        return Ok(await service.AcknowledgeExitAsync(request.input));
    }

    public sealed class StaffSeparationAcknowledgeExitRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationEmployeeAcknowledgeInput input { get; set; } = default!;
    }

    [HttpPost("save-handover-item")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveHandoverItem([FromBody] StaffSeparationSaveHandoverItemRequest request)
    {
        return Ok(await service.SaveHandoverItemAsync(request.input));
    }

    public sealed class StaffSeparationSaveHandoverItemRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationHandoverItemInput input { get; set; } = default!;
    }

    [HttpPost("delete-handover-item")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteHandoverItem([FromBody] StaffSeparationDeleteHandoverItemRequest request)
    {
        return Ok(await service.DeleteHandoverItemAsync(request.workHandoverItemId));
    }

    public sealed class StaffSeparationDeleteHandoverItemRequest
    {
        public global::System.Int32 workHandoverItemId { get; set; }
    }

    [HttpPost("review-handover")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ReviewHandover([FromBody] StaffSeparationReviewHandoverRequest request)
    {
        return Ok(await service.ReviewHandoverAsync(request.input));
    }

    public sealed class StaffSeparationReviewHandoverRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationHandoverReviewInput input { get; set; } = default!;
    }

    [HttpPost("inspect-asset-clearance")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> InspectAssetClearance([FromBody] StaffSeparationInspectAssetClearanceRequest request)
    {
        return Ok(await service.InspectAssetClearanceAsync(request.input));
    }

    public sealed class StaffSeparationInspectAssetClearanceRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationAssetInspectionInput input { get; set; } = default!;
    }

    [HttpPost("save-finance-item")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveFinanceItem([FromBody] StaffSeparationSaveFinanceItemRequest request)
    {
        return Ok(await service.SaveFinanceItemAsync(request.input));
    }

    public sealed class StaffSeparationSaveFinanceItemRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationFinanceItemInput input { get; set; } = default!;
    }

    [HttpPost("delete-finance-item")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteFinanceItem([FromBody] StaffSeparationDeleteFinanceItemRequest request)
    {
        return Ok(await service.DeleteFinanceItemAsync(request.separationFinanceItemId));
    }

    public sealed class StaffSeparationDeleteFinanceItemRequest
    {
        public global::System.Int32 separationFinanceItemId { get; set; }
    }

    [HttpPost("process-clearance-task")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ProcessClearanceTask([FromBody] StaffSeparationProcessClearanceTaskRequest request)
    {
        return Ok(await service.ProcessClearanceTaskAsync(request.input));
    }

    public sealed class StaffSeparationProcessClearanceTaskRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationClearanceTaskActionInput input { get; set; } = default!;
    }

    [HttpPost("process-access-revocation")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ProcessAccessRevocation([FromBody] StaffSeparationProcessAccessRevocationRequest request)
    {
        return Ok(await service.ProcessAccessRevocationAsync(request.input));
    }

    public sealed class StaffSeparationProcessAccessRevocationRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationAccessRevocationActionInput input { get; set; } = default!;
    }

    [HttpPost("final-review-and-execute")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> FinalReviewAndExecute([FromBody] StaffSeparationFinalReviewAndExecuteRequest request)
    {
        return Ok(await service.FinalReviewAndExecuteAsync(request.input));
    }

    public sealed class StaffSeparationFinalReviewAndExecuteRequest
    {
        public global::HRM.DTOs.Separation.StaffSeparationFinalReviewInput input { get; set; } = default!;
    }

    [HttpPost("generate-document")]
    public async Task<ActionResult<global::HRM.DTOs.Separation.SeparationDocumentDto>> GenerateDocument([FromBody] StaffSeparationGenerateDocumentRequest request)
    {
        return Ok(await service.GenerateDocumentAsync(request.staffSeparationId, request.documentVariant));
    }

    public sealed class StaffSeparationGenerateDocumentRequest
    {
        public global::System.Int32 staffSeparationId { get; set; }
        public global::System.String documentVariant { get; set; } = default!;
    }

}
