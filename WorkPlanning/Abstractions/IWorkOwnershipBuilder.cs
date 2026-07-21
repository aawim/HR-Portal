using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Builders;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkOwnershipBuilder
    {
        WorkAssignmentOwner Build(WorkOwnershipBuildContext context);
    }
}
