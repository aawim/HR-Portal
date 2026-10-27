using HRM.DTOs.ApprovalFlow;

namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowRequestContextResolver
    {
        Task<WorkflowRequestContextDto> ResolveAsync(
           int requestId,
           int subjectJobId,
           CancellationToken cancellationToken = default);

        Task<WorkflowRequestContextDto> ResolveAsync(
            HRM.Models.HrmTeContext db,
            int requestId,
            int subjectJobId,
            CancellationToken cancellationToken = default);
    }
}
