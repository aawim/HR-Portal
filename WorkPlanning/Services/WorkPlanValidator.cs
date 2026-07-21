using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Commands;
using HRM.WorkPlanning.Results;

namespace HRM.WorkPlanning.Services
{
    public sealed class WorkPlanValidator : IWorkPlanValidator
    {
        public Task<WorkPlanValidationResult> ValidateCommandAsync(
        BuildWorkPlanCommand command,
        CancellationToken cancellationToken = default)
        {
            var result = new WorkPlanValidationResult();

            if (command == null)
            {
                result.Errors.Add("Build work plan command is required.");
                return Task.FromResult(result);
            }

            if (command.IndividualId <= 0)
                result.Errors.Add("A valid individual is required.");

            if (command.JobId <= 0)
                result.Errors.Add("A valid job is required.");

            if (command.OrganisationBusinessEntityId <= 0)
                result.Errors.Add("A valid organisation is required.");

            if (command.WorkTemplateId <= 0)
                result.Errors.Add("A valid work template is required.");

            return Task.FromResult(result);
        }

        public WorkPlanValidationResult ValidateBuiltPlan(WorkPlan workPlan)
        {
            var result = new WorkPlanValidationResult();

            if (workPlan == null)
            {
                result.Errors.Add("Work plan is required.");
                return result;
            }

            if (workPlan.IndividualId <= 0)
                result.Errors.Add("The work plan has no valid individual.");

            if (workPlan.JobId <= 0)
                result.Errors.Add("The work plan has no valid job.");

            if (workPlan.OrganisationBusinessEntityId <= 0)
                result.Errors.Add("The work plan has no valid organisation.");

            if (workPlan.PlanningProviderId <= 0)
                result.Errors.Add("The work plan has no valid planning provider.");

            if (workPlan.WorkAssignments == null ||
                workPlan.WorkAssignments.Count == 0)
            {
                result.Errors.Add(
                    "The work plan must contain at least one work assignment.");
            }

            return result;
        }

    }
}