using HRM.Models.Workflow;

namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowDefinitionService
    {
        Task<WfWorkflowDefinition> ResolveAsync(
          int requestTypeId,
          int organisationBusinessEntityId,
          int jobTypeId,
          DateOnly effectiveDate,
          CancellationToken cancellationToken = default);
    }
}
