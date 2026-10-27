using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading;

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

            var organisation = await HRM.Helpers.StaffDataAccess.RequireIndividualAsync(db, _access, individualId);
            return await JobHistoryQuery.LoadAsync(db, individualId, organisationId: organisation);
        }
        public async Task<int?> GetOrganisationIdByIndividualAsync(int individualId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var organisation = await HRM.Helpers.StaffDataAccess.RequireIndividualAsync(db, _access, individualId);

            return await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualID == individualId && (!organisation.HasValue || x.OrganisationID == organisation.Value) &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED &&
                    x.TerminatedDate == null)
                .Select(x => (int?)x.OrganisationID)
                .FirstOrDefaultAsync();
        }
        public async Task<ActiveJobDto?> GetActiveJobAsync(int individualId, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var organisation = await HRM.Helpers.StaffDataAccess.RequireIndividualAsync(db, _access, individualId);

            return await db.Jobs
                 .AsNoTracking()
                 .Where(x =>
                     x.IndividualID == individualId && (!organisation.HasValue || x.OrganisationID == organisation.Value) &&
                     x.JobStateId == SharedConfig.JobStates.APPROVED &&
                     x.TerminatedDate == null)
                 .OrderByDescending(x => x.JoinedDate)
                 .ThenByDescending(x => x.JobId)

                 .Select(x => new ActiveJobDto
                {
                    JobId = x.JobId,

                    IndividualId = x.IndividualID,

                    OrganisationId = x.OrganisationID,

                    OrganisationName = x.Organisation.OrganisationName,

                    OrganisationStructureId = x.OrganisationStructureId,

                    OrganisationStructureName = x.OrganisationStructure.Name,

                    JobTypeName = x.JobType.TypeName,

                    JobStateId = x.JobStateId,

                    JobTypeId = x.JobTypeId,

                    JoinedDate = x.JoinedDate,

                    SAPNumber = x.Sapnumber,

                    IsActive = true
                })
               .FirstOrDefaultAsync();
        }
        public async Task<ActiveJobDto?> GetMyActiveJobAsync()
        {
            var context = await _access.RequireContextAsync();

            var job = await GetActiveJobAsync(context.IndividualId);

            if (job == null)
                throw new InvalidOperationException(
                    $"No active job found for IndividualID {context.IndividualId}.");

            return job;
        }
        public async Task<int> GetIndividualIdByJobIdAsync(int jobId)
        {
            if (jobId <= 0)
            {
                return 0; // 1. Return zero instead of null
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            // 2. Removed the (int?) cast so it returns a normal int default (0) if not found
            await HRM.Helpers.StaffDataAccess.RequireJobAsync(db, _access, jobId);
            return await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.JobId == jobId &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED &&
                    x.TerminatedDate == null)
                .Select(x => x.IndividualID)
                .FirstOrDefaultAsync();
        }


    }
}

