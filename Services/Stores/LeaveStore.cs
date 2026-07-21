using HRM.DTOs.Leave;
namespace HRM.Services.Stores
{
    public class LeaveStore
    {
        private readonly UserAppService _service;
        public LeaveStore(UserAppService service)
        {
            _service = service;
        }
        public List<JobLeaveTypeDto> JobLeaveTypes { get; private set; } = new();
        public async Task LoadAsync()
        {
            var data = await _service.GetJobLeaveTypesAsync();
            JobLeaveTypes = data
            .Select(x => new JobLeaveTypeDto
            {
                JobId = x.JobId,
                LeaveTypeID = x.LeaveTypeId,
                LeaveTypeName = x.LeaveType?.Name,
                RemainingDays = x.RemainingDays,
            })
            .ToList();

          
        }
    }
}
