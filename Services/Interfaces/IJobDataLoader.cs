using HRM.DTOs;

namespace HRM.Services.Interfaces
{
    public interface IJobDataLoader
    {
        Task<JobDto?> GetByIdAsync(int jobId);
        Task<List<JobDto>> GetByIndividualIdAsync(int individualId);
    }
}
