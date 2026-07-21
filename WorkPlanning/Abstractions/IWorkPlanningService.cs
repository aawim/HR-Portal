using HRM.WorkPlanning.Commands;
using HRM.WorkPlanning.Results;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanningService
    {
        Task<WorkPlanBuildResult> BuildAndSaveAsync(BuildWorkPlanCommand command,CancellationToken cancellationToken = default);
    }
}
