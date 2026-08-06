using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Interfaces.JobLeaveTypes;

 


namespace HRM.Services.Stores
{
    public class LeaveDataLoader
    {
        private readonly ILeaveService _leaveService;
        private readonly IJobLeaveTypeService _JobLeaveTypes;

        public LeaveDataLoader(ILeaveService leaveService,IJobLeaveTypeService jobLeaveTypeService)
        {
            _leaveService = leaveService;
            _JobLeaveTypes = jobLeaveTypeService;


        }
        public async Task<List<JobLeaveTypeDto>> GetJobLeaveTypes(int jobId)
        {
            return await _JobLeaveTypes.GetJobLeaveTypeByJobId(jobId);
        }

        public async Task<List<LeaveReasonDto>> GetReasons()
        {
            return await _leaveService.GetReasonsByLeaveType();
        }

        public async Task<List<JobLeaveType>> GetLeaveBalances(int jobId)
        {
            return await _leaveService.GetMyLeaveBalancesAsync(jobId);
        }

        public async Task<List<JobLeaveType>> GetUserLeaveBalancesAsync(int jobId)
        {
            return await _leaveService.GetMyLeaveBalancesAsync(jobId);
        }

        public async Task<List<Leaf>> GetLeaveRequestsAsync(int jobId)
        {
            return await _leaveService.GetLeaveRequestsAsync(jobId);
        }


        public async Task<List<JobLeaveType>> GetJobLeaveTypesAsync()
        {
            return await _leaveService.GetJobLeaveTypesAsync();
        }







    }
}
