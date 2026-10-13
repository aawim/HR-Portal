namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowApproverResolver
    {
        Task<IReadOnlyList<WorkflowApproverCandidate>>
        ResolveAsync(
            int workflowStageId,
            int organisationBusinessEntityId,
            CancellationToken cancellationToken = default);
    }

    public sealed record WorkflowApproverCandidate(
        int UserId,
        int StageApproverId,
        int SequenceNumber,
        bool IsRequired
    );
}
