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
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (!actor.IsSuperAdministrator && (org <= 0 || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Use your current organisation.");
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Jobs.AnyAsync(j => j.IndividualID == staffId && (actor.IsSuperAdministrator || j.OrganisationID == org), ct))
            return null;
        if (!actor.IsSuperAdministrator && !await HRM.Helpers.StaffAttributeAccess.CanViewAsync(db, access, staffId, org, ct))
            return null;
        var name = await db.Individuals.AsNoTracking().Where(x => x.BusinessEntityId == staffId)
            .Select(x => (x.FirstNameEnglish ?? "") + " " + (x.MiddleNameEnglish ?? "") + " " + (x.LastNameEnglish ?? "")).FirstOrDefaultAsync(ct);
        return name is null ? null : new(name.Replace("  ", " ").Trim(), await FormerStaffPolicy.IsFormerAsync(db, staffId, org > 0 ? org : 0));
    }

    public async Task<List<StaffJobHistorySummaryDto>> GetStaffJobHistoryAsync(int staffId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (!actor.IsSuperAdministrator && (org <= 0 || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Use your current organisation.");
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!actor.IsSuperAdministrator && !await HRM.Helpers.StaffAttributeAccess.CanViewAsync(db, access, staffId, org, ct))
            return [];

        var today = FormerStaffPolicy.Today;
        var currentJobIds = (await FormerStaffPolicy.CurrentJobs(db)
            .Where(j => j.IndividualID == staffId && (actor.IsSuperAdministrator || j.OrganisationID == org))
            .Select(j => j.JobId)
            .ToListAsync(ct)).ToHashSet();

        var jobs = await db.Jobs.AsNoTracking()
            .Where(j => j.IndividualID == staffId && (actor.IsSuperAdministrator || j.OrganisationID == org))
            .OrderByDescending(j => j.JoinedDate)
            .ThenByDescending(j => j.JobId)
            .Select(j => new
            {
                j.JobId,
                j.OrganisationID,
                OrganisationName = j.Organisation.OrganisationName,
                DepartmentName = j.OrganisationStructure.Name,
                JobTypeName = j.JobType.TypeName,
                StateName = j.JobState.StateName,
                j.JoinedDate,
                j.TerminatedDate
            })
            .ToListAsync(ct);

        if (jobs.Count == 0) return [];

        var jobIds = jobs.Select(j => j.JobId).ToList();
        var orgIds = jobs.Select(j => j.OrganisationID).Distinct().ToList();

        var positions = await db.JobPositions.AsNoTracking()
            .Where(p => jobIds.Contains(p.JobId))
            .OrderByDescending(p => p.JobPositionStateId == 4)
            .ThenByDescending(p => p.FromDate)
            .ThenByDescending(p => p.JobPositionId)
            .Select(p => new { p.JobId, PositionName = p.Position.Name })
            .ToListAsync(ct);

        var positionByJob = positions
            .GroupBy(p => p.JobId)
            .ToDictionary(g => g.Key, g => g.First().PositionName);

        var leaves = await db.Leaves.AsNoTracking()
            .Where(l => jobIds.Contains(l.JobId) && l.LeaveStateId == 4)
            .Select(l => new { l.JobId, l.FromDate, l.ToDate, l.Duration })
            .ToListAsync(ct);

        var workPlans = await db.WorkPlans.AsNoTracking()
            .Where(w => jobIds.Contains(w.JobId) && w.IsValid && w.WorkDate <= today)
            .Select(w => new { w.JobId, Date = w.WorkDate.Date })
            .Distinct()
            .ToListAsync(ct);

        var minDate = jobs.Min(j => j.JoinedDate.Date);
        var attendanceDates = await db.AttendanceLogs.AsNoTracking()
            .Where(a => a.IndividualId == staffId &&
                        orgIds.Contains(a.OrganisationId) &&
                        a.AttendanceLogStateId == HRM.Components.Shared.SharedConfig.AttendanceLogStates.Valid &&
                        a.Date >= minDate &&
                        a.Date <= today.AddDays(1))
            .Select(a => new { a.OrganisationId, Date = a.Date.Date })
            .Distinct()
            .ToListAsync(ct);

        var projectMemberships = (await db.ProjectMembers.AsNoTracking()
            .Where(pm => pm.StaffId == staffId && orgIds.Contains(pm.Project.OrganisationId))
            .Select(pm => new
            {
                pm.Project.OrganisationId,
                pm.ProjectId,
                pm.Project.Reference,
                pm.Project.Name,
                pm.Role,
                pm.Project.Status,
                pm.Project.ProgressPercentage,
                pm.AssignedDate,
                ProjectStartDate = pm.Project.StartDate,
                EndDate = pm.RemovedAt ?? pm.Project.ActualEndDate ?? pm.ExpectedEndDate ?? pm.Project.PlannedEndDate
            })
            .ToListAsync(ct))
            .Select(pm => new
            {
                pm.OrganisationId,
                pm.ProjectId,
                pm.Reference,
                pm.Name,
                pm.Role,
                pm.Status,
                pm.ProgressPercentage,
                StartDate = pm.AssignedDate != default ? pm.AssignedDate : pm.ProjectStartDate,
                pm.EndDate
            })
            .ToList();

        var managedProjects = await db.Projects.AsNoTracking()
            .Where(p => p.ProjectManagerStaffId == staffId && orgIds.Contains(p.OrganisationId))
            .Select(p => new
            {
                p.OrganisationId,
                p.ProjectId,
                p.Reference,
                p.Name,
                Role = "Project Manager",
                p.Status,
                p.ProgressPercentage,
                StartDate = p.StartDate,
                EndDate = p.ActualEndDate ?? p.PlannedEndDate
            })
            .ToListAsync(ct);

        var allProjects = projectMemberships
            .Concat(managedProjects)
            .GroupBy(p => (p.OrganisationId, p.ProjectId))
            .Select(g => g.First())
            .ToList();

        var tasks = await db.ProjectTasks.AsNoTracking()
            .Where(t => t.AssignedStaffId == staffId && orgIds.Contains(t.Project.OrganisationId))
            .Select(t => new
            {
                t.Project.OrganisationId,
                t.TaskId,
                t.TaskReference,
                t.Title,
                ProjectName = t.Project.Name,
                t.Status,
                t.Priority,
                t.Progress,
                StartDate = t.StartDate ?? t.AssignedAt ?? t.CreatedAt,
                DueDate = t.ExpectedEndDate,
                CompletedDate = t.ActualCompletionDate
            })
            .ToListAsync(ct);

        var results = new List<StaffJobHistorySummaryDto>(jobs.Count);
        foreach (var job in jobs)
        {
            var isCurrent = currentJobIds.Contains(job.JobId);
            var startDate = job.JoinedDate.Date;
            var rawEndDate = job.TerminatedDate?.Date;
            var effectiveEnd = rawEndDate.HasValue && rawEndDate.Value < today ? rawEndDate.Value : today;
            var totalDays = effectiveEnd >= startDate ? (effectiveEnd - startDate).Days + 1 : 0;

            var status = !isCurrent && rawEndDate.HasValue && rawEndDate.Value <= today &&
                (job.StateName.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                 job.StateName.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                ? "Terminated"
                : job.StateName;

            var jobLeaves = leaves.Where(l => l.JobId == job.JobId).ToList();
            var leaveDaysTaken = jobLeaves.Sum(l => (decimal)l.Duration);
            var approvedLeaveDates = new HashSet<DateTime>();
            foreach (var l in jobLeaves)
            {
                var lStart = l.FromDate.Date < startDate ? startDate : l.FromDate.Date;
                var lEnd = l.ToDate.Date > effectiveEnd ? effectiveEnd : l.ToDate.Date;
                for (var d = lStart; d <= lEnd; d = d.AddDays(1))
                    approvedLeaveDates.Add(d);
            }

            var presentDates = attendanceDates
                .Where(a => a.OrganisationId == job.OrganisationID && a.Date >= startDate && a.Date <= effectiveEnd)
                .Select(a => a.Date)
                .ToHashSet();

            var scheduledDates = workPlans
                .Where(w => w.JobId == job.JobId && w.Date >= startDate && w.Date <= effectiveEnd)
                .Select(w => w.Date)
                .ToHashSet();

            var absentDays = scheduledDates.Count(d => d < today && !presentDates.Contains(d) && !approvedLeaveDates.Contains(d));

            var jobProjects = allProjects
                .Where(p => p.OrganisationId == job.OrganisationID &&
                            p.StartDate.Date <= effectiveEnd &&
                            (!p.EndDate.HasValue || p.EndDate.Value.Date >= startDate))
                .OrderByDescending(p => p.StartDate)
                .Select(p => new StaffJobProjectSummaryDto(
                    p.ProjectId,
                    p.Reference,
                    p.Name,
                    p.Role,
                    p.Status,
                    p.ProgressPercentage,
                    p.StartDate,
                    p.EndDate))
                .ToList();

            var jobTasks = tasks
                .Where(t => t.OrganisationId == job.OrganisationID &&
                            t.StartDate.Date <= effectiveEnd &&
                            ((t.CompletedDate ?? t.DueDate ?? t.StartDate).Date >= startDate))
                .OrderByDescending(t => t.StartDate)
                .Select(t => new StaffJobTaskSummaryDto(
                    t.TaskId,
                    t.TaskReference,
                    t.Title,
                    t.ProjectName,
                    t.Status,
                    t.Priority,
                    t.Progress,
                    t.DueDate,
                    t.CompletedDate))
                .ToList();

            results.Add(new StaffJobHistorySummaryDto(
                job.JobId,
                job.OrganisationName,
                job.DepartmentName,
                positionByJob.GetValueOrDefault(job.JobId, "No Position Assigned"),
                job.JobTypeName,
                status,
                isCurrent,
                job.JoinedDate,
                job.TerminatedDate,
                totalDays,
                presentDates.Count,
                absentDays,
                scheduledDates.Count,
                leaveDaysTaken,
                jobLeaves.Count,
                jobProjects.Count,
                jobTasks.Count,
                jobTasks.Count(t => t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || t.Progress >= 100),
                jobProjects,
                jobTasks));
        }

        return results;
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
                job != null && currentJobs.Any(j => j.JobId == job.JobId) &&
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
        .Take(50)
        .ToListAsync(token);

        if (!isSuperAdministrator && foundStaff.Count > 0)
        {
            var visible = await HRM.Helpers.StaffAttributeAccess.FilterViewableAsync(Db, access, currentOrganisationId,
                foundStaff.Select(x => x.IndividualId), token);
            foundStaff = foundStaff.Where(x => visible.Contains(x.IndividualId)).Take(10).ToList();
        }
        else
        {
            foundStaff = foundStaff.Take(10).ToList();
        }

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
