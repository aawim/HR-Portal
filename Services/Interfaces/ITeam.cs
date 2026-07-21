using HRM.DTOs;

namespace HRM.Services.Interfaces
{
    public interface ITeam
    {
        Task<List<TeamDto>> GetMyTeamsAsync();
        //Task<List<TeamDto>> GetTeamsAsync(int OrgID);
        //Task<List<TeamDto>> GetTeamsAsync();


        Task<List<TeamDto>> GetMyTeamMembersAsync(int TeamId);






        //Task<List<JobLeaveType>> GetMyLeaveBalancesAsync(int leaveTypeId = 0);
        //Task<List<JobLeaveType>> GetJobLeaveBalancesAsync(int jobId, int leaveTypeId = 0);





        //Task<List<TeamDto>> GetTeamsAsync(int OrgID);
        //Task<List<TeamDto>> GetTeamsAsync();





    }
}
