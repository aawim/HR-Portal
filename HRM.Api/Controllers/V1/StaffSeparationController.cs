using System.Threading.Tasks;
using HRM.Api.Infrastructure;
using HRM.DTOs;
using HRM.DTOs.Separation;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-separation")]
public sealed class StaffSeparationController(global::HRM.Services.Contracts.StaffSeparationServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load-dashboard")]
    public async Task<ActionResult<StaffSeparationDashboardData>> LoadDashboard(
        [FromQuery] int? organisationId = null,
        [FromQuery] string? statusFilter = null,
        [FromQuery] string? search = null)
    {
        return Ok(await service.LoadDashboardAsync(organisationId, statusFilter, search));
    }

    [HttpGet("load-form-lookups")]
    public async Task<ActionResult<StaffSeparationFormLookupsDto>> LoadFormLookups([FromQuery] int? organisationId = null)
    {
        return Ok(await service.LoadFormLookupsAsync(organisationId));
    }

    [HttpGet("load-detail")]
    public async Task<ActionResult<StaffSeparationDetailDto>> LoadDetail(
        [FromQuery] int? staffSeparationId = null,
        [FromQuery] int? individualId = null,
        [FromQuery] int? jobId = null)
    {
        return Ok(await service.LoadDetailAsync(staffSeparationId, individualId, jobId));
    }

    [HttpPost("initiate-separation")]
    public async Task<ActionResult<ServiceResult>> InitiateSeparation([FromBody] StaffSeparationInitiateSeparationRequest request)
    {
        return Ok(await service.InitiateSeparationAsync(request.input));
    }

    public sealed class StaffSeparationInitiateSeparationRequest
    {
        public StaffSeparationInitiateInput input { get; set; } = default!;
    }

    [HttpPost("acknowledge-exit")]
    public async Task<ActionResult<ServiceResult>> AcknowledgeExit([FromBody] StaffSeparationAcknowledgeExitRequest request)
    {
        return Ok(await service.AcknowledgeExitAsync(request.input));
    }

    public sealed class StaffSeparationAcknowledgeExitRequest
    {
        public StaffSeparationEmployeeAcknowledgeInput input { get; set; } = default!;
    }

    [HttpPost("save-handover-item")]
    public async Task<ActionResult<ServiceResult>> SaveHandoverItem([FromBody] StaffSeparationSaveHandoverItemRequest request)
    {
        return Ok(await service.SaveHandoverItemAsync(request.input));
    }

    public sealed class StaffSeparationSaveHandoverItemRequest
    {
        public StaffSeparationHandoverItemInput input { get; set; } = default!;
    }

    [HttpPost("delete-handover-item")]
    public async Task<ActionResult<ServiceResult>> DeleteHandoverItem([FromBody] StaffSeparationDeleteHandoverItemRequest request)
    {
        return Ok(await service.DeleteHandoverItemAsync(request.workHandoverItemId));
    }

    public sealed class StaffSeparationDeleteHandoverItemRequest
    {
        public int workHandoverItemId { get; set; }
    }

    [HttpPost("review-handover")]
    public async Task<ActionResult<ServiceResult>> ReviewHandover([FromBody] StaffSeparationReviewHandoverRequest request)
    {
        return Ok(await service.ReviewHandoverAsync(request.input));
    }

    public sealed class StaffSeparationReviewHandoverRequest
    {
        public StaffSeparationHandoverReviewInput input { get; set; } = default!;
    }

    [HttpPost("inspect-asset-clearance")]
    public async Task<ActionResult<ServiceResult>> InspectAssetClearance([FromBody] StaffSeparationInspectAssetClearanceRequest request)
    {
        return Ok(await service.InspectAssetClearanceAsync(request.input));
    }

    public sealed class StaffSeparationInspectAssetClearanceRequest
    {
        public StaffSeparationAssetInspectionInput input { get; set; } = default!;
    }

    [HttpPost("save-finance-item")]
    public async Task<ActionResult<ServiceResult>> SaveFinanceItem([FromBody] StaffSeparationSaveFinanceItemRequest request)
    {
        return Ok(await service.SaveFinanceItemAsync(request.input));
    }

    public sealed class StaffSeparationSaveFinanceItemRequest
    {
        public StaffSeparationFinanceItemInput input { get; set; } = default!;
    }

    [HttpPost("delete-finance-item")]
    public async Task<ActionResult<ServiceResult>> DeleteFinanceItem([FromBody] StaffSeparationDeleteFinanceItemRequest request)
    {
        return Ok(await service.DeleteFinanceItemAsync(request.separationFinanceItemId));
    }

    public sealed class StaffSeparationDeleteFinanceItemRequest
    {
        public int separationFinanceItemId { get; set; }
    }

    [HttpPost("process-clearance-task")]
    public async Task<ActionResult<ServiceResult>> ProcessClearanceTask([FromBody] StaffSeparationProcessClearanceTaskRequest request)
    {
        return Ok(await service.ProcessClearanceTaskAsync(request.input));
    }

    public sealed class StaffSeparationProcessClearanceTaskRequest
    {
        public StaffSeparationClearanceTaskActionInput input { get; set; } = default!;
    }

    [HttpPost("process-access-revocation")]
    public async Task<ActionResult<ServiceResult>> ProcessAccessRevocation([FromBody] StaffSeparationProcessAccessRevocationRequest request)
    {
        return Ok(await service.ProcessAccessRevocationAsync(request.input));
    }

    public sealed class StaffSeparationProcessAccessRevocationRequest
    {
        public StaffSeparationAccessRevocationActionInput input { get; set; } = default!;
    }

    [HttpPost("final-review-and-execute")]
    public async Task<ActionResult<ServiceResult>> FinalReviewAndExecute([FromBody] StaffSeparationFinalReviewAndExecuteRequest request)
    {
        return Ok(await service.FinalReviewAndExecuteAsync(request.input));
    }

    public sealed class StaffSeparationFinalReviewAndExecuteRequest
    {
        public StaffSeparationFinalReviewInput input { get; set; } = default!;
    }

    [HttpPost("generate-document")]
    public async Task<ActionResult<SeparationDocumentDto>> GenerateDocument([FromBody] StaffSeparationGenerateDocumentRequest request)
    {
        return Ok(await service.GenerateDocumentAsync(request.staffSeparationId, request.documentVariant));
    }

    public sealed class StaffSeparationGenerateDocumentRequest
    {
        public int staffSeparationId { get; set; }
        public string documentVariant { get; set; } = "ExitClearanceCertificate";
    }
}
