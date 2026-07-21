using HRM.Models.WorkPlanning;

namespace HRM.WorkPlanning.Builders
{
    public sealed class WorkAssignmentBuildContext
    {
        public required WorkTemplate Template { get; init; }

        public required DateOnly WorkDate { get; init; }

        public required string AssignmentSource { get; init; }

        public int? CreatedByUserId { get; init; }
    }
}
