using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IManualWorkAssignmentLookupService
    {
        //Task<IReadOnlyList<AssignedWorkTemplateDto>>
        //   GetTemplatesForJobAsync(
        //       int jobId,
        //       DateTime workDate,
        //       CancellationToken cancellationToken = default);

        Task<List<PlanningProviderLookupDto>> GetPlanningProvidersAsync(
          int organisationBusinessEntityId,
          CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ManualAssignmentJobDto>>SearchActiveJobsAsync(
            string? searchText,
            int? organisationId,
            CancellationToken cancellationToken = default);


        Task<WorkAssignmentPreviewDto?> GetAssignmentPreviewAsync(
          int jobId,
          int workTemplateId,
          DateTime workDate,
          CancellationToken cancellationToken = default);



        Task<IReadOnlyList<AssignedWorkTemplateDto>>GetAvailableTemplatesAsync(
            int organisationId,
            DateTime workDate,
            CancellationToken cancellationToken = default);
    }
}
