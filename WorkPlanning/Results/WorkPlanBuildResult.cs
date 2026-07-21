using HRM.Models.WorkPlanning;
namespace HRM.WorkPlanning.Results
{
    public class WorkPlanBuildResult
    {
        public bool IsSuccessful { get; private init; }

        public WorkPlan? WorkPlan { get; private init; }

        public IReadOnlyCollection<string> Errors { get; private init; }
            = Array.Empty<string>();

        public static WorkPlanBuildResult Success(WorkPlan workPlan)
        {
            ArgumentNullException.ThrowIfNull(workPlan);

            return new WorkPlanBuildResult
            {
                IsSuccessful = true,
                WorkPlan = workPlan
            };
        }

        public static WorkPlanBuildResult Failure(params string[] errors)
        {
            return new WorkPlanBuildResult
            {
                IsSuccessful = false,
                Errors = errors
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToArray()
            };
        }

        public static WorkPlanBuildResult Failure(
            IEnumerable<string> errors)
        {
            return Failure(errors.ToArray());
        }
    }
}
