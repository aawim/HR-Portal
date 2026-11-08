using HRM.DTOs;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed class WorkspaceDirectoryService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : IWorkspaceDirectoryService
{
    public async Task<StaffWorkspaceIdentity?> GetStaffIdentityAsync(int staffId, CancellationToken ct = default)
    {
        await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Use your current organisation.");
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Jobs.AnyAsync(j => j.IndividualID == staffId && j.OrganisationID == org, ct)) return null;
        var name = await db.Individuals.AsNoTracking().Where(x => x.BusinessEntityId == staffId)
            .Select(x => (x.FirstNameEnglish ?? "") + " " + (x.MiddleNameEnglish ?? "") + " " + (x.LastNameEnglish ?? "")).FirstOrDefaultAsync(ct);
        return name is null ? null : new(name.Replace("  ", " ").Trim(), await FormerStaffPolicy.IsFormerAsync(db, staffId, org));
    }

    public async Task<WorkspaceSearchPage> SearchAsync(WorkspaceSearchFilter filter, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(filter.SearchText)) return new([], []);
        if (filter.SearchText.Length > 200 || filter.AllowedStaffIds?.Count > 10000) throw new ArgumentException("Invalid search filter.");
       var user = await access.RequireContextAsync();
     
        
        await using var Db = await factory.CreateDbContextAsync(token);
        var searchText = string.Join(" ",filter.SearchText.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
        var restricted = filter.AllowedStaffIds != null;
        var allowedIds = filter.AllowedStaffIds?.ToArray() ?? [];
        var today = DateTime.UtcNow.AddHours(5).Date;
        var currentJobs = FormerStaffPolicy.CurrentJobs(Db);



        var isSuperAdministrator =
            user.IsSuperAdministrator;


            // Never use nullable.Value inside the EF query
            var contextOrganisationId = await access.GetCurrentOrganisationIdAsync();
            var currentOrganisationId = contextOrganisationId;

            if (filter.OrganisationId.HasValue)
            {
                if (filter.OrganisationId <= 0 || (!isSuperAdministrator && filter.OrganisationId != currentOrganisationId))
                    throw new UnauthorizedAccessException("Use your current organisation.");
                currentOrganisationId = filter.OrganisationId.Value;
            }

            if (!isSuperAdministrator && (currentOrganisationId <= 0 || !await access.HasOrganisationAccessAsync(currentOrganisationId)))
                throw new UnauthorizedAccessException("Use your current organisation.");

        // =====================================================
        // STAFF AND INDIVIDUAL SEARCH
        // =====================================================
        var foundStaff = await
        (
            from i in Db.Individuals.AsNoTracking()

            join c in Db.Idcards.AsNoTracking()
                on i.BusinessEntityId equals c.BusinessEntityId
                into cards

            from card in cards.DefaultIfEmpty()

            join j in Db.Jobs.AsNoTracking()
                on i.BusinessEntityId equals j.IndividualID
                into jobs

            from job in jobs.DefaultIfEmpty()

            join os in Db.OrganisationStructures.AsNoTracking()
                on job.OrganisationStructureId
                equals os.OrganisationStructureId
                into structures

            from organisationStructure
                in structures.DefaultIfEmpty()

            let fullName =
                (i.FirstNameEnglish ?? "") + " " +
                (i.MiddleNameEnglish ?? "") + " " +
                (i.LastNameEnglish ?? "")
            let normalisedName = fullName.Replace("  "," ").Trim()

            let fullNameDv =
                (i.FirstNameDhivehi ?? "") +
                (i.LastNameDhivehi != null
                    ? " " + i.LastNameDhivehi
                    : "")

            where (!restricted || allowedIds.Contains(i.BusinessEntityId)) &&
                (job == null || currentJobs.Any(j => j.JobId == job.JobId)) &&
                (!filter.RequireActivePosition || job != null && job.JobStateId == 4 && job.JoinedDate <= today &&
                    (!job.TerminatedDate.HasValue || job.TerminatedDate > today) &&
                    Db.JobPositions.Any(p => p.JobId == job.JobId && p.JobPositionStateId == 4 &&
                        p.FromDate.Date <= today && (!p.ToDate.HasValue || p.ToDate.Value.Date > today))) &&
                (!filter.StaffOnly || job != null && job.OrganisationID == currentOrganisationId && job.JobStateId == 4 && (!job.TerminatedDate.HasValue || job.TerminatedDate > today) && job.JoinedDate <= today) &&
                // Super Administrator: all organisations.
                // Other users: only their active-job organisation.
                (
                    (isSuperAdministrator && !filter.OrganisationId.HasValue) ||
                    (
                        job != null &&
                        job.OrganisationID ==
                            currentOrganisationId
                    )
                )

                &&

                (
                    normalisedName.Contains(searchText)

                    ||

                    (
                        i.FirstNameEnglish != null &&
                        i.FirstNameEnglish.Contains(searchText)
                    )

                    ||

                    (
                        card != null &&
                        card.IdcardNumber != null &&
                        card.IdcardNumber.Contains(searchText)
                    )

                    ||

                    (
                        organisationStructure != null &&
                        organisationStructure.Name != null &&
                        organisationStructure.Name.Contains(searchText)
                    )
                )

            select new WorkspaceSearchResult
            {
                Id = i.BusinessEntityId,
                IndividualId = i.BusinessEntityId,

                JobId = job != null
                    ? job.JobId
                    : null,

                Name = normalisedName,
                NameDv = fullNameDv,

                IDCard =
                    card != null &&
                    card.IdcardNumber != null
                        ? card.IdcardNumber
                        : "",

                Description = job == null
                    ? "Individual"
                    : "Staff" +
                      (
                          organisationStructure != null
                              ? " | " +
                                organisationStructure.Name
                              : ""
                      ),

                EntityType = job == null
                    ? WorkspaceEntityType.Individual
                    : WorkspaceEntityType.Staff
            }
        )
        .Distinct()
        .OrderBy(x => x.Name)
        .Take(10)
        .ToListAsync(token);

        // =====================================================
        // ORGANISATION SEARCH
        // =====================================================
        var organisationQuery = Db.Organisations
            .AsNoTracking()
            .AsQueryable();

        if (!isSuperAdministrator)
        {
            var organisationId =
                currentOrganisationId!;

            organisationQuery =
                organisationQuery.Where(x =>
                    x.BusinessEntityID == organisationId);
        }

        var foundOrganisations = filter.StaffOnly ? new List<WorkspaceSearchResult>() : await organisationQuery
            .Where(x =>
                x.OrganisationName != null &&
                x.OrganisationName.Contains(searchText))
            .OrderBy(x => x.OrganisationName)
            .Take(10)
            .Select(x => new WorkspaceSearchResult
            {
                Id = x.BusinessEntityID,
                Name = x.OrganisationName,
                EntityType =
                    WorkspaceEntityType.Organisation
            })
            .ToListAsync(token);

        return new(foundStaff, foundOrganisations);
    }
}
