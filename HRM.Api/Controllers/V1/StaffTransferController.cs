using HRM.Api.Infrastructure;
using HRM.DTOs;
using HRM.DTOs.StaffTransfer;
using HRM.Enums;
using HRM.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-transfer")]
public sealed class StaffTransferController : HrmApiController
{
    private readonly StaffTransferServiceContracts.IOperations _service;

    public StaffTransferController(StaffTransferServiceContracts.IOperations service)
    {
        _service = service;
    }

    [HttpGet("load-dashboard")]
    public Task<StaffTransferDashboardData> LoadDashboardAsync(
        [FromQuery] int? organisationId = null,
        [FromQuery] string? statusFilter = null)
        => _service.LoadDashboardAsync(organisationId, statusFilter);

    [HttpGet("load-form-lookups")]
    public Task<StaffTransferFormLookupDto> LoadFormLookupsAsync(
        [FromQuery] int? organisationId = null,
        [FromQuery] int? individualId = null,
        [FromQuery] int? destinationOrganisationId = null)
        => _service.LoadFormLookupsAsync(organisationId, individualId, destinationOrganisationId);

    [HttpGet("load-detail")]
    public Task<StaffTransferDetailDto> LoadDetailAsync(
        [FromQuery] int? staffTransferId = null,
        [FromQuery] int? individualId = null,
        [FromQuery] int? jobId = null)
        => _service.LoadDetailAsync(staffTransferId, individualId, jobId);

    [HttpPost("save")]
    public Task<ServiceResult> SaveAsync([FromBody] SaveRequest request)
        => _service.SaveAsync(request.input);

    [HttpPost("add-comment")]
    public Task<ServiceResult> AddCommentAsync([FromBody] AddCommentRequest request)
        => _service.AddCommentAsync(
            request.staffTransferId,
            request.commentKind,
            request.commentText,
            request.isVisibleToEmployee);

    [HttpPost("submit-employee-confirmation")]
    public Task<ServiceResult> SubmitEmployeeConfirmationAsync([FromBody] SubmitEmployeeConfirmationRequest request)
        => _service.SubmitEmployeeConfirmationAsync(request.input);

    [HttpPost("submit-receiving-decision")]
    public Task<ServiceResult> SubmitReceivingDecisionAsync([FromBody] SubmitReceivingDecisionRequest request)
        => _service.SubmitReceivingDecisionAsync(request.input);

    [HttpPost("activate-transfer")]
    public Task<TransferActivationResult> ActivateTransferAsync([FromBody] ActivateTransferRequest request)
        => _service.ActivateTransferAsync(request.staffTransferId);

    [HttpPost("update-door-access-provisioning")]
    public Task<ServiceResult> UpdateDoorAccessProvisioningAsync([FromBody] UpdateDoorAccessProvisioningRequest request)
        => _service.UpdateDoorAccessProvisioningAsync(
            request.staffTransferDoorAccessId,
            request.provisioningState,
            request.notes);

    [HttpGet("get-printable-document")]
    public Task<StaffTransferPrintableDocumentDto> GetPrintableDocumentAsync(
        [FromQuery] int staffTransferId,
        [FromQuery] string variant = StaffTransferDocumentVariants.EmployeeNotice)
        => _service.GetPrintableDocumentAsync(staffTransferId, variant);

    public sealed class SaveRequest
    {
        public StaffTransferSaveInput input { get; set; } = new();
    }

    public sealed class AddCommentRequest
    {
        public int staffTransferId { get; set; }
        public string commentKind { get; set; } = StaffTransferCommentKinds.AdvisoryComment;
        public string commentText { get; set; } = string.Empty;
        public bool isVisibleToEmployee { get; set; } = true;
    }

    public sealed class SubmitEmployeeConfirmationRequest
    {
        public StaffTransferEmployeeConfirmationInput input { get; set; } = new();
    }

    public sealed class SubmitReceivingDecisionRequest
    {
        public StaffTransferDecisionInput input { get; set; } = new();
    }

    public sealed class ActivateTransferRequest
    {
        public int staffTransferId { get; set; }
    }

    public sealed class UpdateDoorAccessProvisioningRequest
    {
        public int staffTransferDoorAccessId { get; set; }
        public string provisioningState { get; set; } = StaffTransferDoorAccessStates.StateProvisioned;
        public string? notes { get; set; }
    }
}
