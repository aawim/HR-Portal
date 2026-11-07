// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-transfer")]
public sealed class StaffTransferController(global::HRM.Services.Contracts.StaffTransferServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load-dashboard")]
    public async Task<ActionResult<global::HRM.DTOs.StaffTransfer.StaffTransferDashboardData>> LoadDashboard([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.String? statusFilter = null)
    {
        return Ok(await service.LoadDashboardAsync(organisationId, statusFilter));
    }

    [HttpGet("load-form-lookups")]
    public async Task<ActionResult<global::HRM.DTOs.StaffTransfer.StaffTransferFormLookupDto>> LoadFormLookups([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Int32? individualId = null, [FromQuery] global::System.Int32? destinationOrganisationId = null)
    {
        return Ok(await service.LoadFormLookupsAsync(organisationId, individualId, destinationOrganisationId));
    }

    [HttpGet("load-detail")]
    public async Task<ActionResult<global::HRM.DTOs.StaffTransfer.StaffTransferDetailDto>> LoadDetail([FromQuery] global::System.Int32? staffTransferId = null, [FromQuery] global::System.Int32? individualId = null, [FromQuery] global::System.Int32? jobId = null)
    {
        return Ok(await service.LoadDetailAsync(staffTransferId, individualId, jobId));
    }

    [HttpPost("save")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Save([FromBody] StaffTransferSaveRequest request)
    {
        return Ok(await service.SaveAsync(request.input));
    }

    public sealed class StaffTransferSaveRequest
    {
        public global::HRM.DTOs.StaffTransfer.StaffTransferSaveInput input { get; set; } = default!;
    }

    [HttpPost("add-comment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AddComment([FromBody] StaffTransferAddCommentRequest request)
    {
        return Ok(await service.AddCommentAsync(request.staffTransferId, request.commentKind, request.commentText, request.isVisibleToEmployee));
    }

    public sealed class StaffTransferAddCommentRequest
    {
        public global::System.Int32 staffTransferId { get; set; }
        public global::System.String commentKind { get; set; } = default!;
        public global::System.String commentText { get; set; } = default!;
        public global::System.Boolean isVisibleToEmployee { get; set; } = true;
    }

    [HttpPost("submit-employee-confirmation")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitEmployeeConfirmation([FromBody] StaffTransferSubmitEmployeeConfirmationRequest request)
    {
        return Ok(await service.SubmitEmployeeConfirmationAsync(request.input));
    }

    public sealed class StaffTransferSubmitEmployeeConfirmationRequest
    {
        public global::HRM.DTOs.StaffTransfer.StaffTransferEmployeeConfirmationInput input { get; set; } = default!;
    }

    [HttpPost("submit-receiving-decision")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitReceivingDecision([FromBody] StaffTransferSubmitReceivingDecisionRequest request)
    {
        return Ok(await service.SubmitReceivingDecisionAsync(request.input));
    }

    public sealed class StaffTransferSubmitReceivingDecisionRequest
    {
        public global::HRM.DTOs.StaffTransfer.StaffTransferDecisionInput input { get; set; } = default!;
    }

    [HttpPost("activate-transfer")]
    public async Task<ActionResult<global::HRM.DTOs.StaffTransfer.TransferActivationResult>> ActivateTransfer([FromBody] StaffTransferActivateTransferRequest request)
    {
        return Ok(await service.ActivateTransferAsync(request.staffTransferId));
    }

    public sealed class StaffTransferActivateTransferRequest
    {
        public global::System.Int32 staffTransferId { get; set; }
    }

    [HttpPost("update-door-access-provisioning")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UpdateDoorAccessProvisioning([FromBody] StaffTransferUpdateDoorAccessProvisioningRequest request)
    {
        return Ok(await service.UpdateDoorAccessProvisioningAsync(request.staffTransferDoorAccessId, request.provisioningState, request.notes));
    }

    public sealed class StaffTransferUpdateDoorAccessProvisioningRequest
    {
        public global::System.Int32 staffTransferDoorAccessId { get; set; }
        public global::System.String provisioningState { get; set; } = default!;
        public global::System.String? notes { get; set; }
    }

    [HttpGet("get-printable-document")]
    public async Task<ActionResult<global::HRM.DTOs.StaffTransfer.StaffTransferPrintableDocumentDto>> GetPrintableDocument([FromQuery] global::System.Int32 staffTransferId, [FromQuery] global::System.String variant = "EmployeeNotice")
    {
        return Ok(await service.GetPrintableDocumentAsync(staffTransferId, variant));
    }

}
