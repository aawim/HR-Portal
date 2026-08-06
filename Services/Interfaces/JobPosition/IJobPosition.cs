using HRM.DTOs.JobPosition;
using HRM.Models;

namespace HRM.Services.Interfaces.JobPosition
{
    public interface IJobPosition
    {
      
        Task<JobPositionDto?> GetCurrentPositionAsync(
            int jobId, CancellationToken cancellationToken = default);

        Task<List<JobPositionHistoryDto>> GetPositionHistoryByJobAsync(int jobId,CancellationToken cancellationToken = default);


        //Task<List<JobPositionHistoryDto>>GetPositionHistoryAsync(int individualId,
        //     int organisationId,
        //     CancellationToken cancellationToken = default);



        Task<List<JobHistoryWithPositionsDto>> GetStaffJobHistoryAsync(
         int individualId,
         int organisationId,
         CancellationToken cancellationToken = default);




        //Task<List<JobPositionDto>> GetHistoryAsync(
        //    int jobId,
        //    CancellationToken cancellationToken = default);


        //Task<ServiceResult> RequestPositionAsync(
        //    CreateJobPositionRequest request,
        //    CancellationToken cancellationToken = default);

        //Task<ServiceResult> RequestRemovalAsync(
        //    RemoveJobPositionRequest request,
        //    CancellationToken cancellationToken = default);
    }
}
