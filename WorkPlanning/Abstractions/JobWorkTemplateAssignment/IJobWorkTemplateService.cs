using HRM.DTOs;
using HRM.DTOs.Job;
using HRM.DTOs.Job.JobWorkTemplateAssignment;
using HRM.Models;

namespace HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment
{
    public interface IJobWorkTemplateService
    {
        Task<ServiceResult> AssignAsync(AssignJobWorkTemplateDto dto);

        Task<List<JobWorkTemplateDto>> GetByJobAsync(int jobId);

        Task<JobWorkTemplate?> GetEffectiveAssignmentAsync(
            int jobId,
            DateTime workDate);
    }
}
