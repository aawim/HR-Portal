using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanService
    {
        Task<List<WorkPlanDto>> GetWorkPlansAsync(
           int individualId,
           DateTime fromDate,
           DateTime toDate);

        Task<WorkPlanDto?> GetWorkPlanAsync(int workPlanId);

        Task<List<WorkPlanEmployeeDto>> SearchEmployeesAsync(string searchText);

        Task<List<WorkTemplateLookupDto>> GetActiveTemplatesAsync();

        Task<GenerateWorkPlanResultDto> GenerateWorkPlanAsync(GenerateWorkPlanDto request);

        Task<List<PlanningProviderLookupDto>>GetPlanningProvidersAsync();
    }
}
