using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanService
    {
        Task<GenerateWorkPlanResultDto> GenerateWorkPlanAsync(GenerateWorkPlanDto request);

        Task<List<WorkPlanDto>> GetWorkPlansAsync(int individualId,
            DateTime fromDate,
            DateTime toDate);

        Task<WorkPlanDto?> GetWorkPlanAsync(int workPlanId);
    }
}
