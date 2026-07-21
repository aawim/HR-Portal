using HRM.DTOs.WorkPlanning;
using HRM.WorkPlanning.Abstractions;

namespace HRM.WorkPlanning.Services.TestServicesToBeRemoved
{
    public class WorkAssignmentResolverTestService
    {
        private readonly IWorkAssignmentResolver _resolver;

        public WorkAssignmentResolverTestService(
            IWorkAssignmentResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<WorkAssignmentResolutionResult> TestAsync(
            int individualId)
        {
            return await _resolver.ResolveAsync(
                individualId,
                DateTime.Now);
        }
    }
}
