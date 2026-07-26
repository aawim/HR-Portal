using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanService
    {
        Task<List<WorkPlanListDto>> GetWorkPlansAsync(
           //int individualId,
           DateTime fromDate,
           DateTime toDate);

        Task<WorkPlanListDto?> GetWorkPlanAsync(int workPlanId);

        Task<List<WorkPlanEmployeeDto>> SearchEmployeesAsync(string searchText);

        Task<List<WorkTemplateLookupDto>> GetActiveTemplatesAsync();

        Task<GenerateWorkPlanResultDto> GenerateWorkPlanAsync(GenerateWorkPlanDto request);

        Task<List<PlanningProviderLookupDto>>GetPlanningProvidersAsync();
    }
}
