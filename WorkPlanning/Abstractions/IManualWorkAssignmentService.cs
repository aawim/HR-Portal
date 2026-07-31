using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IManualWorkAssignmentService
    {
        Task<GeneratedWorkPlanResult> GenerateAsync(
        ManualWorkAssignmentRequest request,
        int generatedByUserId,
        CancellationToken cancellationToken = default);
    }
}
