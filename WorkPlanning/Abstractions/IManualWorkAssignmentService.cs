using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IManualWorkAssignmentService
    {
        Task<GeneratedWorkPlanResult> UpdateAsync(long assignmentId, ManualWorkAssignmentRequest request, CancellationToken cancellationToken = default);
        Task<GeneratedWorkPlanResult> CancelAsync(long assignmentId, string reason, CancellationToken cancellationToken = default);
        //Task<GeneratedWorkPlanResult> GenerateAsync(
        //ManualWorkAssignmentRequest request,
        //int generatedByUserId,
        //CancellationToken cancellationToken = default);

        Task<GeneratedWorkPlanResult> GenerateAsync(
            ManualWorkAssignmentRequest request,
            CancellationToken cancellationToken = default);
    }
}
