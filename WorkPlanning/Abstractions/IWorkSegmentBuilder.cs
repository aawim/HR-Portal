using HRM.Models.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkSegmentBuilder
    {
        IReadOnlyCollection<WorkAssignmentSegment> BuildSegments(WorkTemplate template,
       WorkAssignment assignment);
    }
}
