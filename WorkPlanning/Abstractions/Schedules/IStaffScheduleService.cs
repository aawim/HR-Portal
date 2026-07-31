using HRM.DTOs.StaffSchedule;

namespace HRM.WorkPlanning.Abstractions.Schedules
{
    public interface IStaffScheduleService
    {
        Task<StaffScheduleResult> GetScheduleAsync(
          int individualId,
          CancellationToken cancellationToken = default);
    }
}
