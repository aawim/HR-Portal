using HRM.DTOs.ApprovalFlow;

namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowRequestContextResolver
    {
        Task<WorkflowRequestContextDto> ResolveAsync(
           int requestId,
           CancellationToken cancellationToken = default);
    }
}
