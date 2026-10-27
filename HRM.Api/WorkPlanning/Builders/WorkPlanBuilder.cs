using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Commands;
using HRM.WorkPlanning.Results;

namespace HRM.WorkPlanning.Builders
{
    public sealed class WorkPlanBuilder : IWorkPlanBuilder
    {
        public async Task<WorkPlanBuildResult> BuildAsync(
            BuildWorkPlanCommand command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            await Task.CompletedTask;

            return WorkPlanBuildResult.Failure(
                "Work plan builder implementation is not complete yet.");
        }
    }
}
