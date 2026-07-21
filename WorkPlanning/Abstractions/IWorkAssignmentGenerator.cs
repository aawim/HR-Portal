using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkAssignmentGenerator
    {
        Task<GeneratedWorkPlanResult> GenerateAsync(
        GenerateWorkPlanRequest request,
        CancellationToken cancellationToken = default);

        Task<BulkAssignmentResult> AssignDepartmentAsync(
          BulkDepartmentAssignmentRequest request,
          CancellationToken cancellationToken = default);
    }
}
