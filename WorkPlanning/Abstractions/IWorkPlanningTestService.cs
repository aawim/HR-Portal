using HRM.DTOs.WorkPlanning;
namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanningTestService
    {
        Task<WorkPlanTestResult> GenerateStandardOfficePlanAsync(
        int organisationId,
        int individualId,
        int operationLogId,
        DateTime workDate,
        CancellationToken cancellationToken = default);
    }
}
