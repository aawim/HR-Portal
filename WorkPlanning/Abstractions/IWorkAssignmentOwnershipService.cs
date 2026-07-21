using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkAssignmentOwnershipService
    {
        Task<WorkAssignmentOwnershipResult> AssignAsync(
            AssignWorkAssignmentRequest request,
            CancellationToken cancellationToken = default);
        Task<WorkAssignmentOwnershipResult> TransferAsync(
           TransferWorkAssignmentRequest request,
           CancellationToken cancellationToken = default);

        Task<WorkAssignmentOwnershipResult> RelieveAsync(
            RelieveWorkAssignmentRequest request,
            CancellationToken cancellationToken = default);

        Task<WorkAssignmentOwnershipResult> TakeOverAsync(
            TakeOverWorkAssignmentRequest request,
            CancellationToken cancellationToken = default);

        Task<WorkAssignmentTransferStateDto> GetTransferStateAsync(
            long workAssignmentId,
            CancellationToken cancellationToken = default);
    }
}
