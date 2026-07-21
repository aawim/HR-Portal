using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class JobService : IJobService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
            private readonly IUserAccessService _access;
        public JobService(IDbContextFactory<HrmTeContext> dbFactory, IUserAccessService userAccessService )
        {
            _dbFactory = dbFactory;
            _access = userAccessService;

        }


        public async Task<List<JobDto>> GetMyJobHistoryAsync()
        {
            var context = await _access.RequireContextAsync();

            return await GetJobHistoryAsync(context.IndividualId);
        }




        public async Task<List<JobDto>> GetJobHistoryAsync(int individualId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualID == individualId &&
                    !(x.JobStateId == SharedConfig.JobStates.APPROVED &&
                      x.TerminatedDate == null))
                .OrderByDescending(x => x.JoinedDate)
                .Select(x => new JobDto
                {
                    JobID = x.JobId,

                    IndividualID = x.IndividualID,

                    OrganisationID = x.OrganisationID,

                    OrganisationStructureID = x.OrganisationStructureId,

                    JobStateID = x.JobStateId,

                    JobTypeID = x.JobTypeId,

                    JoinedDate = x.JoinedDate,

                    TerminatedDate = x.TerminatedDate,

                    BasicSalary = x.BasicSalary
                })
                .ToListAsync();
        }



        public async Task<int?> GetOrganisationIdByIndividualAsync(
    int individualId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualID == individualId &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED &&
                    x.TerminatedDate == null)
                .Select(x => (int?)x.OrganisationID)
                .FirstOrDefaultAsync();
        }


        public async Task<ActiveJobDto?> GetActiveJobAsync(int individualId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();


            return await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualID == individualId
                    &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED
                    &&
                    x.TerminatedDate == null)
                .Select(x => new ActiveJobDto
                {
                    JobID = x.JobId,

                    IndividualID = x.IndividualID,

                    OrganisationID = x.OrganisationID,

                    OrganisationName = x.Organisation.OrganisationName,

                    OrganisationStructureID = x.OrganisationStructureId,

                    OrganisationStructureName = x.OrganisationStructure.Name,

                    JobTypeName = x.JobType.TypeName,

                    JobStateID = x.JobStateId,

                    JobTypeID = x.JobTypeId,

                    JoinedDate = x.JoinedDate,

                    SAPNumber = x.Sapnumber,

                    IsActive = true
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ActiveJobDto> GetMyActiveJobAsync()
        {
            var context = await _access.RequireContextAsync();

            var job = await GetActiveJobAsync(context.IndividualId);

            if (job == null)
                throw new InvalidOperationException(
                    $"No active job found for IndividualID {context.IndividualId}.");

            return job;
        }

       






        //private readonly IJobDataLoader _loader;
        //private readonly IJobStore _store;
        //private readonly UserService _userService;

        //public JobService(
        //    IJobDataLoader loader,
        //    IJobStore store,
        //    UserService userService)
        //{
        //    _loader = loader;
        //    _store = store;
        //    _userService = userService;
        //}

        //public async Task<JobDto?> GetCurrentJobAsync()
        //{
        //    var jobId = await _userService.GetJobID();
        //    if (jobId == null)
        //        return null;
        //    return await _loader.GetByIdAsync(jobId);
        //}

        //public async Task<JobDto?> GetActiveJobAsync(int individualId)
        //{
        //    var jobs = await _loader.GetByIndividualIdAsync(individualId);
        //    return jobs.FirstOrDefault(x =>
        //        x.JobStateID == 4 &&
        //        x.TerminatedDate == null);
        //}

        //public async Task<List<JobDto>> GetActiveJobsAsync(int individualId)
        //{
        //    var jobs = await _loader.GetByIndividualIdAsync(individualId);
        //    return jobs
        //        .Where(x => x.JobStateID == 4 && x.TerminatedDate == null)
        //        .ToList();
        //}



    }
}
