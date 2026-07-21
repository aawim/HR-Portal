using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Builders;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkAssignmentBuilder
    {
        WorkAssignment Build(WorkAssignmentBuildContext context);
    }
}
