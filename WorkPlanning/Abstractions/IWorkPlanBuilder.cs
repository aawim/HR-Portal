using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Commands;
using HRM.WorkPlanning.Results;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanBuilder
    {
        Task<WorkPlanBuildResult> BuildAsync(BuildWorkPlanCommand command,CancellationToken cancellationToken = default);
    }
}
