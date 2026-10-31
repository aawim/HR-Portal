using HRM.DTOs;

namespace HRM.Services.Interfaces
{
    public interface ITeam
    {
        Task<List<TeamDto>> GetMyTeamsAsync();
        Task<int> CreateAsync(int organisationId, string name, string? nameDhivehi, DateTime startDate, CancellationToken ct = default);
        //Task<List<TeamDto>> GetTeamsAsync(int OrgID);
        //Task<List<TeamDto>> GetTeamsAsync();

        Task<List<TeamDto>> GetMyTeamMembersAsync(int TeamId);

        Task<int> AddStaffAsync(int teamId, int staffId, DateTime startDate, bool isSupervisor = false, CancellationToken ct = default);
        Task<bool> RemoveStaffAsync(int teamId, int staffId, CancellationToken ct = default);

        //Task<List<JobLeaveType>> GetMyLeaveBalancesAsync(int leaveTypeId = 0);
        //Task<List<JobLeaveType>> GetJobLeaveBalancesAsync(int jobId, int leaveTypeId = 0);


        //Task<List<TeamDto>> GetTeamsAsync(int OrgID);
        //Task<List<TeamDto>> GetTeamsAsync();


    }
}