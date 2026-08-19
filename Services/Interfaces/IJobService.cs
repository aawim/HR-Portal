using HRM.DTOs;
using HRM.DTOs.UserContext;


namespace HRM.Services.Interfaces
{
    public interface IJobService
    {

        Task<ActiveJobDto?> GetActiveJobAsync(int individualId, CancellationToken cancellationToken = default);

        Task<ActiveJobDto?> GetMyActiveJobAsync();

        Task<List<JobDto>> GetMyJobHistoryAsync();

        Task<int?> GetOrganisationIdByIndividualAsync(int individualId);

        Task<int> GetIndividualIdByJobIdAsync(int jobId);

    }
}
