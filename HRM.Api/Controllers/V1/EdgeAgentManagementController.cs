using HRM.Api.Infrastructure;
using HRM.DTOs;
using HRM.DTOs.EdgeAttendance;
using HRM.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/edge-agents")]
public sealed class EdgeAgentManagementController(EdgeAgentManagementContracts.IOperations service) : HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EdgeAgentSummaryDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpGet("organisations")]
    public async Task<ActionResult<IReadOnlyList<HRM.DTOs.Organisation.OrganisationSettingsOption>>> ListOrganisations(CancellationToken ct) =>
        Ok(await service.ListOrganisationsAsync(ct));

    [HttpGet("devices")]
    public async Task<ActionResult<IReadOnlyList<EdgeAttendanceDeviceDto>>> ListDevices(CancellationToken ct) =>
        Ok(await service.ListDevicesAsync(ct));

    [HttpGet("failures")]
    public async Task<ActionResult<IReadOnlyList<EdgeAttendanceIngestionFailureDto>>> ListFailures(CancellationToken ct) =>
        Ok(await service.ListFailuresAsync(ct));

    [HttpGet("trusted-issuers")]
    public async Task<ActionResult<IReadOnlyList<EdgeTrustedIssuerDto>>> ListTrustedIssuers(CancellationToken ct) =>
        Ok(await service.ListTrustedIssuersAsync(ct));

    [HttpPost("trusted-issuers")]
    public async Task<ActionResult<ServiceResult>> AddTrustedIssuer(TrustedIssuerRequest request, CancellationToken ct) =>
        Ok(await service.AddTrustedIssuerAsync(request.input, ct));

    [HttpPost("trusted-issuers/revoke")]
    public async Task<ActionResult<ServiceResult>> RevokeTrustedIssuer(RevokeIssuerRequest request, CancellationToken ct) =>
        Ok(await service.RevokeTrustedIssuerAsync(request.issuerId, ct));

    [HttpPost("devices/save")]
    public async Task<ActionResult<ServiceResult>> SaveDevice(SaveDeviceRequest request, CancellationToken ct) =>
        Ok(await service.SaveDeviceConfigurationAsync(request.input, ct));

    [HttpPost]
    public async Task<ActionResult<ServiceResult>> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await service.RegisterAsync(request.input, ct));

    [HttpPost("revoke")]
    public async Task<ActionResult<ServiceResult>> Revoke(RevokeRequest request, CancellationToken ct) =>
        Ok(await service.RevokeAsync(request.agentId, ct));

    [HttpPost("reconcile")]
    public async Task<ActionResult<EdgeReconciliationResultDto>> ReevaluatePendingReceipts(ReconcileRequest request, CancellationToken ct) =>
        Ok(await service.ReevaluatePendingReceiptsAsync(request.input, ct));

    [HttpPost("pending-receipts")]
    public async Task<ActionResult<IReadOnlyList<EdgePendingReceiptDto>>> ListPendingReceipts(ListPendingRequest request, CancellationToken ct) =>
        Ok(await service.ListPendingReceiptsAsync(request.input, ct));

    [HttpPost("pending-receipts/resolve")]
    public async Task<ActionResult<ServiceResult>> ResolvePendingReceipt(ResolvePendingReceiptRequest request, CancellationToken ct) =>
        Ok(await service.ResolvePendingReceiptAsync(request.input, ct));

    [HttpPost("staff-search")]
    public async Task<ActionResult<IReadOnlyList<EdgeStaffOptionDto>>> SearchStaff(SearchStaffRequest request, CancellationToken ct) =>
        Ok(await service.SearchStaffAsync(request.input, ct));

    [HttpPost("activation-codes")]
    public async Task<ActionResult<CreatedActivationCodeDto>> CreateActivationCode(CreateActivationCodeRequest request, CancellationToken ct) =>
        Ok(await service.CreateActivationCodeAsync(request.input, ct));

    [HttpGet("activation-codes")]
    public async Task<ActionResult<IReadOnlyList<EdgeActivationCodeDto>>> ListActivationCodes(CancellationToken ct) =>
        Ok(await service.ListActivationCodesAsync(ct));

    [HttpPost("activation-codes/revoke")]
    public async Task<ActionResult<ServiceResult>> RevokeActivationCode(RevokeActivationCodeRequest request, CancellationToken ct) =>
        Ok(await service.RevokeActivationCodeAsync(request.activationCodeId, ct));

    [HttpPost("commands")]
    public async Task<ActionResult<ServiceResult>> QueueCommand(QueueCommandRequest request, CancellationToken ct) =>
        Ok(await service.QueueCommandAsync(request.input, ct));

    [HttpGet("commands")]
    public async Task<ActionResult<IReadOnlyList<EdgeConnectorCommandDto>>> ListCommands(CancellationToken ct) =>
        Ok(await service.ListCommandsAsync(ct));

    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<EdgeConnectorAlertDto>>> ListAlerts(CancellationToken ct) =>
        Ok(await service.ListAlertsAsync(ct));

    public sealed class TrustedIssuerRequest { public EdgeTrustedIssuerInput input { get; set; } = new(); }
    public sealed class RevokeIssuerRequest { public int issuerId { get; set; } }
    public sealed class SaveDeviceRequest { public EdgeAttendanceDeviceConfigurationInput input { get; set; } = new(); }
    public sealed class RegisterRequest { public EdgeAgentRegistrationInput input { get; set; } = new(); }
    public sealed class RevokeRequest { public int agentId { get; set; } }
    public sealed class ReconcileRequest { public EdgeReconciliationFilterInput input { get; set; } = new(); }
    public sealed class ListPendingRequest { public EdgeReconciliationFilterInput input { get; set; } = new(); }
    public sealed class ResolvePendingReceiptRequest { public ResolvePendingReceiptInput input { get; set; } = new(); }
    public sealed class SearchStaffRequest { public EdgeStaffSearchInput input { get; set; } = new(); }
    public sealed class CreateActivationCodeRequest { public CreateActivationCodeInput input { get; set; } = new(); }
    public sealed class RevokeActivationCodeRequest { public int activationCodeId { get; set; } }
    public sealed class QueueCommandRequest { public QueueConnectorCommandInput input { get; set; } = new(); }
}
