using HRM.DTOs.StaffSchedule;
using HRM.DTOs;
using HRM.DTOs.Job.JobWorkTemplateAssignment;
using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment
{
    public interface IJobWorkTemplateAssignmentService
    {
        Task<CurrentWorkTemplateAssignmentDto?> GetCurrentAsync(
        int jobId,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);

        Task<List<JobWorkTemplateAssignmentHistoryDto>> GetHistoryAsync(
            int jobId,
            CancellationToken cancellationToken = default);

        Task<List<WorkTemplateLookupDto>> SearchTemplatesAsync(
            int organisationBusinessEntityId,
            string? searchText,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> AssignAsync(
            AssignJobWorkTemplateDto dto,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> RemoveAsync(
            RemoveJobWorkTemplateDto dto,
            CancellationToken cancellationToken = default);
    }
}
