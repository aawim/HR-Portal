using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkAssignmentResolver
    {
        Task<WorkAssignmentResolutionResult> ResolveAsync(
            int individualId,
            DateTime attendanceDateTime,
            CancellationToken cancellationToken = default);
    }
}
