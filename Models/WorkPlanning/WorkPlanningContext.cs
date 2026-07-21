using HRM.WorkPlanning.Commands;

namespace HRM.Models.WorkPlanning
{
    public sealed class WorkPlanningContext
    {
        public BuildWorkPlanCommand Command { get; init; }

        public PlanningProvider Provider { get; set; }

        public WorkTemplate Template { get; set; }

        public WorkPlan WorkPlan { get; set; }

        public List<string> Warnings { get; } = new();

        public List<string> Errors { get; } = new();
    }
}
