using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Services.Interfaces;


namespace HRM.Services.Stores
{
    public class LeaveDataLoader
    {
        private readonly ILeaveService _leaveService;
   
        public LeaveDataLoader(ILeaveService leaveService)
        {
            _leaveService = leaveService;

        }
        public async Task<List<JobLeaveTypeDto>> GetJobLeaveTypes(int jobId)
        {
            return await _leaveService.GetJobLeaveTypeByJobId(jobId);
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
