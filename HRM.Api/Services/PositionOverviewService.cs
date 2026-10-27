using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class PositionOverviewService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
:global::HRM.Services.Contracts.PositionOverviewServiceContracts,global::HRM.Services.Contracts.PositionOverviewServiceContracts.IOperations{
    public async Task<PositionOverviewData> LoadAsync()
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && !actor.IsHRStaff && !actor.IsHRHead && !actor.IsHRManager && !actor.IsOrganisationAdmin &&
            !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Position.Details") &&
            !await access.HasPermissionAsync("Staff.Positions.Manage"))
            throw new UnauthorizedAccessException("You do not have permission to view positions and staff assignments.");
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("Select an organisation you can access.");
        await using var db = await factory.CreateDbContextAsync();
        var today = DateTime.UtcNow.AddHours(5).Date;
        // Include unscoped legacy records only when actually used by this tenant.
        var positions = await db.Positions.AsNoTracking().Where(p => p.IsActive != false && (p.OrganisationId == org || p.IsGlobal ||
                (p.OrganisationId == null && db.JobPositions.Any(a => a.PositionId == p.PositionId && a.Job.OrganisationID == org))))
            .OrderBy(p => p.Name).Select(p => new PositionOverviewRow { Id = p.PositionId, Name = p.Name,
                IsActive = p.IsActive != false, IsGlobal = p.IsGlobal }).ToListAsync();
        var ids = positions.Select(p => p.Id).ToList();
        var assignments = await (from a in db.JobPositions.AsNoTracking().Where(HRM.Helpers.PositionLifecycle.ApprovedOn(today))
                                 join job in db.Jobs.AsNoTracking() on a.JobId equals job.JobId
                                 join person in db.Individuals.AsNoTracking() on job.IndividualID equals person.BusinessEntityId
                                 where ids.Contains(a.PositionId) && job.OrganisationID == org &&
                                     job.JobStateId == 4 && job.JoinedDate.Date <= today &&
                                     (!job.TerminatedDate.HasValue || job.TerminatedDate.Value.Date > today)
                                 orderby person.FirstNameEnglish, person.LastNameEnglish, a.FromDate descending
                                 select new { a.PositionId, a.JobId, a.FromDate, a.ToDate, a.JobPositionStateId,
                                     AssignmentState = a.JobPositionState.StateName, job.JobStateId, EmploymentState = job.JobState.StateName,
                                     job.JoinedDate, job.TerminatedDate, job.IndividualID,
                                     person.FirstNameEnglish, person.MiddleNameEnglish, person.LastNameEnglish,
                                     Department = job.OrganisationStructure.Name }).ToListAsync();
        var grouped = assignments.GroupBy(a => a.PositionId).ToDictionary(g => g.Key, g => g.Select(a =>
        {
            return new PositionOverviewStaff { IndividualId = a.IndividualID, JobId = a.JobId,
                Name = string.Join(" ", new[] { a.FirstNameEnglish, a.MiddleNameEnglish, a.LastNameEnglish }
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim())),
                Department = a.Department ?? "", From = a.FromDate, To = a.ToDate, IsCurrent = true, Status = "Current" };
        }).ToList());
        foreach (var position in positions) position.Staff = grouped.GetValueOrDefault(position.Id, []);
        return new PositionOverviewData { Organisation = await db.Organisations.Where(x => x.BusinessEntityID == org)
            .Select(x => x.OrganisationName).SingleAsync(), Positions = positions };
    }
}
