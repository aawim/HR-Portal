using HRM.Components.Shared;
using HRM.Models;
using HRM.Resources;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class IndividualResourceBuilder : IIndividualResourceBuilder
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public IndividualResourceBuilder(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        public async Task<IndividualResource?> BuildAsync(int individualId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();


            var job = await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualID == individualId &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED &&
                    x.TerminatedDate == null)
                .Select(x => new
                {
                    x.JobId,
                    x.IndividualID,
                    x.OrganisationID,
                    x.OrganisationStructureId
                })
                .FirstOrDefaultAsync();


            if (job == null)
                return null;


            return new IndividualResource
            {
                JobID = job.JobId,

                IndividualID = job.IndividualID,

                OrganisationID = job.OrganisationID,

                OrganisationStructureID =
                    job.OrganisationStructureId,

                IsActive = true
            };
        }
    }
}