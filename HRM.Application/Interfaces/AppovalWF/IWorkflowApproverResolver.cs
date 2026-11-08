using HRM.DTOs.ApprovalFlow;

namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowApproverResolver
    {
        Task<IReadOnlyList<WorkflowApproverCandidate>>
        ResolveAsync(
            int workflowStageId,
            WorkflowRequestContextDto context,
            CancellationToken cancellationToken = default);
    }

    public sealed record WorkflowApproverCandidate(
        int UserId,
        int StageApproverId,
        int SequenceNumber,
        bool IsRequired
    );
}
