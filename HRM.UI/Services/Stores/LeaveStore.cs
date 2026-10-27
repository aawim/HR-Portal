using HRM.DTOs.Leave;
namespace HRM.Services.Stores
{
    public class LeaveStore
    {
        private readonly HRM.Services.Interfaces.IUserAppDataService _service;
        public LeaveStore(HRM.Services.Interfaces.IUserAppDataService service)
        {
            _service = service;
        }
        public List<JobLeaveTypeDto> JobLeaveTypes { get; private set; } = new();
        public async Task LoadAsync()
        {
            JobLeaveTypes = await _service.GetJobLeaveTypesAsync();

        }
    }
}
