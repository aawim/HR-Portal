using HRM.DTOs.StaffTransfer;

namespace HRM.Services.Interfaces;

/// <summary>
/// Applies an approved internal staff transfer safely and idempotently on or after its effective date.
/// </summary>
public interface IStaffTransferActivationService
{
    Task<TransferActivationResult> ActivateAsync(
        int staffTransferId,
        CancellationToken cancellationToken = default);
}
