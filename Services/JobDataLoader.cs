using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class JobDataLoader : IJobDataLoader
    {
       

        private readonly IDbContextFactory<HrmTeContext> _context;

        public JobDataLoader(IDbContextFactory<HrmTeContext> factory)
        {
            _context = factory;
        }



        public async Task<JobDto?> GetByIdAsync(int jobId)
        {
            using var context = _context.CreateDbContext();
            return await context.Jobs
                .Where(x => x.JobId == jobId)
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
                    BasicSalary = x.BasicSalary,
                    SAPNumber = x.Sapnumber,
                    ServiceID = x.ServiceId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<JobDto>> GetByIndividualIdAsync(int individualId)
        {

            using var context = _context.CreateDbContext();
            return await context.Jobs
                .Where(x => x.IndividualID == individualId)
                .Include(o => o.Organisation)
                .Include(os => os.OrganisationStructure)
                .Include(jt => jt.JobType)
                .Select(x => new JobDto
                {
                    JobID = x.JobId,
                    IndividualID = x.IndividualID,
                    OrganisationID = x.OrganisationID,
                    OrganisationName = x.Organisation.OrganisationName,
                    OrganisationStructureID = x.OrganisationStructureId,
                    OrganisationStructureName = x.OrganisationStructure.Name,
                    JobStateID = x.JobStateId,
                    JobTypeID = x.JobTypeId,
                    JobTypeName = x.JobType.TypeName,
                    JoinedDate = x.JoinedDate,
                    TerminatedDate = x.TerminatedDate,
                    BasicSalary = x.BasicSalary,
                    SAPNumber = x.Sapnumber,
                    ServiceID = x.ServiceId,
                })
                .ToListAsync();
        }
    }
}
