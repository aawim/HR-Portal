using HRM.Contracts;
using HRM.DTOs;
using HRM.Enums;
using HRM.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/staff-transfer")]
public sealed class StaffTransferController : ControllerBase
{
    private readonly StaffTransferServiceContracts.IOperations _service;

    public StaffTransferController(StaffTransferServiceContracts.IOperations service)
    {
        _service = service;
    }

    [HttpGet("load-dashboard")]
    public Task<StaffTransferDashboardDto> LoadDashboardAsync(
        [FromQuery] int? organisationId = null,
        [FromQuery] StaffTransferStatuses? statusFilter = null,
        CancellationToken cancellationToken = default)
        => _service.LoadDashboardAsync(organisationId, statusFilter, cancellationToken);

    [HttpGet("load-form-lookups")]
    public Task<StaffTransferFormLookupDto> LoadFormLookupsAsync(
        [FromQuery] int? organisationId = null,
        [FromQuery] int? individualId = null,
        [FromQuery] int? destinationOrganisationId = null,
        CancellationToken cancellationToken = default)
        => _service.LoadFormLookupsAsync(organisationId, individualId, destinationOrganisationId, cancellationToken);

    [HttpGet("load-detail")]
    public Task<StaffTransferDetailDto?> LoadDetailAsync(
        [FromQuery] int? staffTransferId = null,
        [FromQuery] int? individualId = null,
        [FromQuery] int? jobId = null,
        CancellationToken cancellationToken = default)
        => _service.LoadDetailAsync(staffTransferId, individualId, jobId, cancellationToken);

    [HttpPost("save")]
    public Task<ServiceResult> SaveAsync(
        [FromBody] SaveRequest request,
        CancellationToken cancellationToken = default)
        => _service.SaveAsync(request.input, cancellationToken);

    [HttpPost("add-comment")]
    public Task<ServiceResult> AddCommentAsync(
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken = default)
        => _service.AddCommentAsync(
            request.staffTransferId,
            request.commentKind,
            request.commentText,
            request.isVisibleToEmployee,
            cancellationToken);

    [HttpPost("submit-employee-confirmation")]
    public Task<ServiceResult> SubmitEmployeeConfirmationAsync(
        [FromBody] SubmitEmployeeConfirmationRequest request,
        CancellationToken cancellationToken = default)
        => _service.SubmitEmployeeConfirmationAsync(request.input, cancellationToken);

    [HttpPost("submit-receiving-decision")]
    public Task<ServiceResult> SubmitReceivingDecisionAsync(
        [FromBody] SubmitReceivingDecisionRequest request,
        CancellationToken cancellationToken = default)
        => _service.SubmitReceivingDecisionAsync(request.input, cancellationToken);

    [HttpPost("activate-transfer")]
    public Task<TransferActivationResult> ActivateTransferAsync(
        [FromBody] ActivateTransferRequest request,
        CancellationToken cancellationToken = default)
        => _service.ActivateTransferAsync(request.staffTransferId, cancellationToken);

    [HttpPost("update-door-access-provisioning")]
    public Task<ServiceResult> UpdateDoorAccessProvisioningAsync(
        [FromBody] UpdateDoorAccessProvisioningRequest request,
        CancellationToken cancellationToken = default)
        => _service.UpdateDoorAccessProvisioningAsync(
            request.staffTransferDoorAccessId,
            request.provisioningState,
            request.notes,
            cancellationToken);

    [HttpGet("get-printable-document")]
    public Task<StaffTransferPrintableDocumentDto?> GetPrintableDocumentAsync(
        [FromQuery] int staffTransferId,
        [FromQuery] StaffTransferDocumentVariants variant = StaffTransferDocumentVariants.EmployeeNotice,
        CancellationToken cancellationToken = default)
        => _service.GetPrintableDocumentAsync(staffTransferId, variant, cancellationToken);

    public sealed class SaveRequest
    {
        public StaffTransferSaveInput input { get; set; } = new();
    }

    public sealed class AddCommentRequest
    {
        public int staffTransferId { get; set; }
        public StaffTransferCommentKinds commentKind { get; set; }
        public string commentText { get; set; } = string.Empty;
        public bool isVisibleToEmployee { get; set; } = true;
    }

    public sealed class SubmitEmployeeConfirmationRequest
    {
        public StaffTransferConfirmationInput input { get; set; } = new();
    }

    public sealed class SubmitReceivingDecisionRequest
    {
        public StaffTransferReceivingDecisionInput input { get; set; } = new();
    }

    public sealed class ActivateTransferRequest
    {
        public int staffTransferId { get; set; }
    }

    public sealed class UpdateDoorAccessProvisioningRequest
    {
        public int staffTransferDoorAccessId { get; set; }
        public StaffTransferDoorAccessStates provisioningState { get; set; }
        public string? notes { get; set; }
    }
}
