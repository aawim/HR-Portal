using HRM.Components.Shared;
using HRM.DTOs;
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
    public async Task<(List<OrganisationWorkspaceStaff> Rows, int Total)> StaffAsync(int organisationId, string search, int departmentId, int page)
    {
        await RequireAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var today = Today;
        var query = db.Jobs.AsNoTracking().Where(x => x.OrganisationID == organisationId && x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= today);
        if (departmentId > 0) query = query.Where(x => x.OrganisationStructureId == departmentId);
        search = search.Trim();
        if (search.Length > 0) query = query.Where(x => (x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? "")).Contains(search) ||
            (x.Staff.EmployeeNumber != null && x.Staff.EmployeeNumber.Contains(search)) || (x.Sapnumber != null && x.Sapnumber.Contains(search)));
        var rows = query.Select(x => new
        {
            Id = x.IndividualID,
            Name = (x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? "")).Trim(),
            Department = x.OrganisationStructure.Name, EmployeeNumber = x.Staff.EmployeeNumber, SapNumber = x.Sapnumber, Joined = x.JoinedDate
        }).Distinct();
        var total = await rows.CountAsync();
        var pageRows = await rows.OrderBy(x => x.Name).ThenBy(x => x.Id).ThenBy(x => x.Joined).Skip(Math.Max(0, page) * 20).Take(20).ToListAsync();
        return (pageRows.Select(x => new OrganisationWorkspaceStaff(x.Id, x.Name, x.Department, x.EmployeeNumber, x.SapNumber, x.Joined)).ToList(), total);
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
