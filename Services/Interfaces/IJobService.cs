using HRM.DTOs;
using HRM.DTOs.UserContext;


namespace HRM.Services.Interfaces
{
    public interface IJobService
    {

        Task<ActiveJobDto?> GetActiveJobAsync(int individualId);

        Task<ActiveJobDto?> GetMyActiveJobAsync();

        Task<List<JobDto>> GetMyJobHistoryAsync();

        Task<int?> GetOrganisationIdByIndividualAsync(int individualId);
    }
}
