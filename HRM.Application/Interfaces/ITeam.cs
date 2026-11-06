using HRM.DTOs;

namespace HRM.Services.Interfaces
{
    public interface ITeam
    {
        Task<List<TeamDto>> GetMyTeamsAsync();
        Task<int> CreateAsync(int organisationId, string name, string? nameDhivehi, DateTime startDate, CancellationToken ct = default);
        Task<List<TeamDto>> GetMyTeamMembersAsync(int TeamId);
        Task<int> AddStaffAsync(int teamId, int staffId, DateTime startDate, bool isSupervisor = false, CancellationToken ct = default);
        Task<bool> RemoveStaffAsync(int teamId, int staffId, CancellationToken ct = default);
        Task<List<TeamSearchItemDto>> SearchTeamsAsync(string query, CancellationToken ct = default);
        Task<TeamDto?> GetTeamByIdAsync(int teamId, CancellationToken ct = default);
        Task<List<TeamProjectDto>> GetTeamProjectsAsync(int teamId, CancellationToken ct = default);
        Task<TeamProjectDto?> GetProjectDetailsAsync(int projectId, CancellationToken ct = default);
        Task<TeamProjectTaskDto?> GetTaskDetailsAsync(int taskId, CancellationToken ct = default);
        Task<Staff360SummaryDto?> GetStaff360Async(int staffId, CancellationToken ct = default);
        Task<List<StaffEvaluationDto>> GetStaffEvaluationsAsync(int staffId, CancellationToken ct = default);
        Task<int> SaveStaffEvaluationAsync(SaveEvaluationRequestDto request, CancellationToken ct = default);
        Task<TeamDto?> GetHelpDeskTeamAsync(CancellationToken ct = default);
    }
}
