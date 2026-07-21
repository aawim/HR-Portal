using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Commands;
using HRM.WorkPlanning.Results;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanValidator
    {
        Task<WorkPlanValidationResult> ValidateCommandAsync(BuildWorkPlanCommand command,
       CancellationToken cancellationToken = default);

        WorkPlanValidationResult ValidateBuiltPlan(WorkPlan workPlan);
    }
}
