using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class OrganisationWorkspaceService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
{
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
