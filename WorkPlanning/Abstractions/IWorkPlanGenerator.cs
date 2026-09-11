using HRM.DTOs.Attendance;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkPlanGenerator
    {
        Task<AttendanceWorkPlanDto?> GenerateOrGetAsync(
       int individualId,
       int jobId,
       int organisationBusinessEntityId,
       DateOnly workDate,
       CancellationToken cancellationToken = default);
    }
}
