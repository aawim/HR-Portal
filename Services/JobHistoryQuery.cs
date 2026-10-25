using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

internal static class JobHistoryQuery
{
    public static Task<List<JobDto>> LoadAsync(HrmTeContext db, int individualId, CancellationToken token = default, int? organisationId = null)
        => (from job in db.Jobs.AsNoTracking()
            join organisation in db.Organisations on job.OrganisationID equals organisation.BusinessEntityID into organisations
            from organisation in organisations.DefaultIfEmpty()
            join structure in db.OrganisationStructures on job.OrganisationStructureId equals structure.OrganisationStructureId into structures
            from structure in structures.DefaultIfEmpty()
            join state in db.JobStates on job.JobStateId equals state.JobStateId into states
            from state in states.DefaultIfEmpty()
            join type in db.JobTypes on job.JobTypeId equals type.JobTypeId into types
            from type in types.DefaultIfEmpty()
            where job.IndividualID == individualId && (!organisationId.HasValue || job.OrganisationID == organisationId.Value)
            orderby job.JoinedDate descending, job.JobId descending
            select new JobDto
            {
                JobID = job.JobId, IndividualID = job.IndividualID, OrganisationID = job.OrganisationID,
                OrganisationName = organisation == null ? "Not recorded" : organisation.OrganisationName,
                OrganisationStructureID = job.OrganisationStructureId,
                OrganisationStructureName = structure == null ? "Not recorded" : structure.Name,
                JobStateID = job.JobStateId, JobStateName = state == null ? "Not recorded" : state.StateName,
                JobTypeID = job.JobTypeId, JobTypeName = type == null ? "Not recorded" : type.TypeName,
                JoinedDate = job.JoinedDate, TerminatedDate = job.TerminatedDate,
                BasicSalary = job.BasicSalary, SAPNumber = job.Sapnumber, ServiceID = job.ServiceId
            }).ToListAsync(token);
}
