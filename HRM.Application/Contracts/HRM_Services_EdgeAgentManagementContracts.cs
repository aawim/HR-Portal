using HRM.DTOs;
using HRM.DTOs.EdgeAttendance;

namespace HRM.Services.Contracts;

public abstract class EdgeAgentManagementContracts
{
    public interface IOperations
    {
        Task<IReadOnlyList<EdgeAgentSummaryDto>> ListAsync(CancellationToken ct = default);
        Task<IReadOnlyList<HRM.DTOs.Organisation.OrganisationSettingsOption>> ListOrganisationsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<EdgeAttendanceDeviceDto>> ListDevicesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<EdgeAttendanceIngestionFailureDto>> ListFailuresAsync(CancellationToken ct = default);
        Task<IReadOnlyList<EdgeTrustedIssuerDto>> ListTrustedIssuersAsync(CancellationToken ct = default);
        Task<ServiceResult> AddTrustedIssuerAsync(EdgeTrustedIssuerInput input, CancellationToken ct = default);
        Task<ServiceResult> RevokeTrustedIssuerAsync(int issuerId, CancellationToken ct = default);
        Task<ServiceResult> SaveDeviceConfigurationAsync(EdgeAttendanceDeviceConfigurationInput input, CancellationToken ct = default);
        Task<ServiceResult> RegisterAsync(EdgeAgentRegistrationInput input, CancellationToken ct = default);
        Task<ServiceResult> RevokeAsync(int agentId, CancellationToken ct = default);
        Task<EdgeReconciliationResultDto> ReevaluatePendingReceiptsAsync(EdgeReconciliationFilterInput input, CancellationToken ct = default);
        Task<IReadOnlyList<EdgePendingReceiptDto>> ListPendingReceiptsAsync(EdgeReconciliationFilterInput input, CancellationToken ct = default);
        Task<ServiceResult> ResolvePendingReceiptAsync(ResolvePendingReceiptInput input, CancellationToken ct = default);
        Task<IReadOnlyList<EdgeStaffOptionDto>> SearchStaffAsync(EdgeStaffSearchInput input, CancellationToken ct = default);
        Task<CreatedActivationCodeDto> CreateActivationCodeAsync(CreateActivationCodeInput input, CancellationToken ct = default);
        Task<IReadOnlyList<EdgeActivationCodeDto>> ListActivationCodesAsync(CancellationToken ct = default);
        Task<ServiceResult> RevokeActivationCodeAsync(int activationCodeId, CancellationToken ct = default);
        Task<ServiceResult> QueueCommandAsync(QueueConnectorCommandInput input, CancellationToken ct = default);
        Task<IReadOnlyList<EdgeConnectorCommandDto>> ListCommandsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<EdgeConnectorAlertDto>> ListAlertsAsync(CancellationToken ct = default);
    }
}
