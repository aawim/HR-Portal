using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class OrganisationWorkspaceService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
:global::HRM.Services.Contracts.OrganisationWorkspaceServiceContracts,global::HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations{
    private static DateTime Today => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;
    private async Task RequireAsync(int organisationId)
    {
        var actor = await access.RequireContextAsync();
        if (organisationId <= 0 || (!actor.IsSuperAdministrator && !await access.HasOrganisationAccessAsync(organisationId)))
            throw new UnauthorizedAccessException("You do not have access to this organisation.");
    }
    public async Task<OrganisationWorkspaceData> LoadAsync(int organisationId)
    {
        await RequireAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var today = Today;
        var data = await db.Organisations.AsNoTracking().Where(x => x.BusinessEntityID == organisationId)
            .Select(x => new OrganisationWorkspaceData { Name = x.OrganisationName, DhivehiName = x.OrganisationNameDhivehi,
                RegistrationNumber = x.RegistrationNumber, Type = x.OrganisationType.TypeName,
                Parent = x.ParentOrganisationBusinessEntity == null ? null : x.ParentOrganisationBusinessEntity.OrganisationName }).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Organisation not found.");
        var jobs = db.Jobs.AsNoTracking().Where(x => x.OrganisationID == organisationId && x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= today);
        var staffIds = await jobs.Select(x => x.IndividualID).Distinct().ToListAsync();
        var viewableStaffIds = await StaffAttributeAccess.FilterViewableAsync(db, access, organisationId, staffIds);
        if (viewableStaffIds.Count < staffIds.Count)
            jobs = jobs.Where(x => viewableStaffIds.Contains(x.IndividualID));
        data.StaffCount = await jobs.Select(x => x.IndividualID).Distinct().CountAsync();
        data.Workforce = await jobs.Select(x => new OrganisationOverviewStaff(x.IndividualID, x.OrganisationStructureId,
            x.JobTypeId, x.JobType.TypeName, x.Individual.GenderTypeId)).Distinct().ToListAsync();
        var homeTypeNames = new[] { "work from home", "work-from-home", "working from home", "wfh" };
        var homeTypes = await db.OutOfficeRequestTypes.AsNoTracking()
            .Where(t => homeTypeNames.Contains(t.TypeName.Trim().ToLower()))
            .Select(t => t.OutOfOfficeRequestTypeId).ToListAsync();
        data.HasWorkFromHomeRequestType = homeTypes.Count > 0;
        data.WorkingFromHomeStaffIds = await db.OutOfOfficeRequests.AsNoTracking()
            .Where(r => r.OrganisationBusinessEntityID == organisationId && homeTypes.Contains(r.OutOfOfficeRequestTypeId) &&
                r.Request.OrganisationBusinessEntityID == organisationId && r.Request.RequestStateId == SharedConfig.RequestStates.APPROVED &&
                r.Date >= today && r.Date < today.AddDays(1) && jobs.Any(j => j.IndividualID == r.StaffId))
            .Select(r => r.StaffId).Distinct().ToListAsync();
        data.OnLeaveStaffIds = await db.Leaves.AsNoTracking()
            .Where(l => jobs.Any(j => j.JobId == l.JobId) && l.LeaveStateId == SharedConfig.LeaveStates.APPROVED && l.FromDate < today.AddDays(1) && l.ToDate >= today)
            .Select(l => l.Job.IndividualID).Distinct().ToListAsync();
        // Leave names are HR data: require the canonical leave-view permission before returning them.
        var actor = await access.RequireContextAsync();
        data.CanViewLeaveDetails = actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Leave.View");
        if (data.CanViewLeaveDetails)
            data.StaffOnLeave = await db.Leaves.AsNoTracking()
                .Where(l => jobs.Any(j => j.JobId == l.JobId) && l.LeaveStateId == SharedConfig.LeaveStates.APPROVED && l.FromDate < today.AddDays(1) && l.ToDate >= today)
                .OrderBy(l => l.ToDate).ThenBy(l => l.LeaveId)
                .Select(l => new OrganisationOverviewLeave(l.LeaveId, l.Job.IndividualID, l.Job.OrganisationStructureId, l.Job.JobTypeId,
                    (l.Job.Individual.FirstNameEnglish + " " + (l.Job.Individual.MiddleNameEnglish ?? "") + " " + (l.Job.Individual.LastNameEnglish ?? "")).Trim(),
                    l.LeaveType.Name, l.FromDate, l.ToDate, l.Duration)).ToListAsync();
        data.Departments = await db.OrganisationStructures.AsNoTracking().Where(x => x.OrganisationBusinessEntityID == organisationId && x.OrganisationStructureStateId == 3)
            .OrderBy(x => x.Name).Select(x => new OrganisationWorkspaceGroup(x.OrganisationStructureId, x.Name,
                jobs.Where(j => j.OrganisationStructureId == x.OrganisationStructureId).Select(j => j.IndividualID).Distinct().Count())).ToListAsync();
        data.Teams = await db.Teams.AsNoTracking().Where(x => x.OrganisationId == organisationId && x.IsValid == true &&
            (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today))
            .OrderBy(x => x.Name).Select(x => new OrganisationWorkspaceGroup(x.TeamId, x.Name,
                x.TeamStaffs.Where(m => m.IsValid && m.StartDate <= today && (m.EndDate == null || m.EndDate >= today) && jobs.Any(j => j.IndividualID == m.StaffId))
                    .Select(m => m.StaffId).Distinct().Count())).ToListAsync();
        return data;
    }
    public async Task<(List<OrganisationWorkspaceStaff> Rows, int Total)> StaffAsync(int organisationId, string search, int departmentId, int page, string scope = "active")
    {
        await RequireAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var today = Today;
        search = search.Trim();
        if (string.Equals(scope, "individuals", StringComparison.OrdinalIgnoreCase))
        {
            var people = db.Individuals.AsNoTracking().Where(x => !db.Jobs.Any(j => j.IndividualID == x.BusinessEntityId));
            if (search.Length > 0) people = people.Where(x => (x.FirstNameEnglish + " " + (x.MiddleNameEnglish ?? "") + " " + (x.LastNameEnglish ?? "")).Contains(search));
            var totalIndividuals = await people.CountAsync();
            var pageIndividuals = await people.OrderBy(x => x.FirstNameEnglish).ThenBy(x => x.BusinessEntityId).Skip(Math.Max(0, page) * 20).Take(20)
                .Select(x => new OrganisationWorkspaceStaff(x.BusinessEntityId, (x.FirstNameEnglish + " " + (x.MiddleNameEnglish ?? "") + " " + (x.LastNameEnglish ?? "")).Trim(), "Not assigned to an organisation", null, null, DateTime.MinValue, "—")).ToListAsync();
            return (pageIndividuals, totalIndividuals);
        }
        var activeJobs = db.Jobs.AsNoTracking().Where(x =>
            x.OrganisationID == organisationId &&
            x.JobStateId == SharedConfig.JobStates.APPROVED &&
            x.JoinedDate <= today &&
            (!x.TerminatedDate.HasValue || x.TerminatedDate > today));

        IQueryable<Job> query;
        if (string.Equals(scope, "former", StringComparison.OrdinalIgnoreCase))
        {
            var endedJobs = db.Jobs.AsNoTracking().Where(x =>
                x.OrganisationID == organisationId &&
                !activeJobs.Any(a => a.IndividualID == x.IndividualID) &&
                (x.JobStateId != SharedConfig.JobStates.APPROVED || x.JoinedDate > today || (x.TerminatedDate != null && x.TerminatedDate <= today)));

            query = endedJobs.Where(x =>
                !endedJobs.Any(n => n.IndividualID == x.IndividualID &&
                    (n.JoinedDate > x.JoinedDate || (n.JoinedDate == x.JoinedDate && n.JobId > x.JobId))));
        }
        else
        {
            query = activeJobs.Where(x =>
                !activeJobs.Any(n => n.IndividualID == x.IndividualID &&
                    (n.JoinedDate > x.JoinedDate || (n.JoinedDate == x.JoinedDate && n.JobId > x.JobId))));
        }
        var staffIds = await query.Select(x => x.IndividualID).Distinct().ToListAsync();
        var viewableStaffIds = await StaffAttributeAccess.FilterViewableAsync(db, access, organisationId, staffIds);
        if (viewableStaffIds.Count < staffIds.Count)
            query = query.Where(x => viewableStaffIds.Contains(x.IndividualID));
        if (departmentId > 0) query = query.Where(x => x.OrganisationStructureId == departmentId);
        if (search.Length > 0) query = query.Where(x => (x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? "")).Contains(search) ||
            (x.Staff.EmployeeNumber != null && x.Staff.EmployeeNumber.Contains(search)) || (x.Sapnumber != null && x.Sapnumber.Contains(search)));
        var rows = query.Select(x => new
        {
            Id = x.IndividualID,
            Name = (x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? "")).Trim(),
            Department = x.OrganisationStructure.Name, EmployeeNumber = x.Staff.EmployeeNumber, SapNumber = x.Sapnumber, Joined = x.JoinedDate,
            Position = x.JobPositions.Where(p => p.JobPositionStateId == SharedConfig.JobPositionStates.APPROVED && p.FromDate <= today && (p.ToDate == null || p.ToDate >= today)).OrderByDescending(p => p.FromDate).Select(p => p.Position.Name).FirstOrDefault() ?? "—"
        }).Distinct();
        var total = await rows.CountAsync();
        var pageRows = await rows.OrderBy(x => x.Name).ThenBy(x => x.Id).ThenBy(x => x.Joined).Skip(Math.Max(0, page) * 20).Take(20).ToListAsync();
        return (pageRows.Select(x => new OrganisationWorkspaceStaff(x.Id, x.Name, x.Department, x.EmployeeNumber, x.SapNumber, x.Joined, x.Position)).ToList(), total);
    }
    public async Task<(List<OrganisationWorkspaceDocument> Rows, int Total)> DocumentsAsync(int organisationId, int page)
    {
        await RequireAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var query = db.BusinessEntitiesDocuments.AsNoTracking().Where(x => x.BusinessEntityId == organisationId && !x.Document.IsDeleted);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.LinkedDate).ThenBy(x => x.DocumentId).Skip(Math.Max(0, page) * 20).Take(20)
            .Select(x => new OrganisationWorkspaceDocument(x.DocumentId, x.Document.DocumentName ?? "Untitled document", x.Document.DocumentType.TypeName, x.Document.DocumentDate, x.Document.DocumentSize)).ToListAsync();
        return (rows, total);
    }
}
