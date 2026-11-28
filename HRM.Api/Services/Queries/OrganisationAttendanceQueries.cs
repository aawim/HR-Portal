using HRM.Application.Contracts.Pages;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Queries;

public sealed class OrganisationAttendanceQueries(IDbContextFactory<HrmTeContext> factory,
    IAttendanceService attendance, ApiReadScope scope, IUserAccessService? userAccess = null) : IOrganisationAttendanceQueries
{
    public async Task<OrganisationAttendancePage> GetDailyAsync(OrganisationAttendanceFilter filter, CancellationToken ct = default)
    {
        await scope.RequireStaffReadAsync("Attendance.View");
        var organisation = await scope.OrganisationAsync(filter.OrganisationId);
        var day = ApiReadScope.Dates(filter.Date, filter.Date).From;
        await using var db = await factory.CreateDbContextAsync(ct);
        var orgScope = userAccess is not null
            ? await OrganisationalAccessScopeResolver.ResolveCurrentUserAsync(db, userAccess, organisation, "Attendance.View", ct)
            : null;
        var structures = await db.OrganisationStructures.AsNoTracking().Where(s => s.OrganisationBusinessEntityID == organisation && s.OrganisationStructureStateId == 3 &&
                (!filter.StructureTypeId.HasValue || s.OrganisationStructureTypeId == filter.StructureTypeId))
            .OrderBy(s => s.Name).Select(s => new DirectoryOption(s.OrganisationStructureId, s.Name)).ToListAsync(ct);
        var teams = await db.Teams.AsNoTracking().Where(t => t.OrganisationId == organisation && t.IsValid == true)
            .OrderBy(t => t.Name).Select(t => new { t.TeamId, t.Name, t.OrganisationStructureId }).ToListAsync(ct);
        if (orgScope is not null && !orgScope.HasFullOrganisationScope)
        {
            structures = structures.Where(s => orgScope.CanAccessUnit(s.Id)).ToList();
            teams = teams.Where(t => orgScope.CanAccessTeam(t.TeamId, t.OrganisationStructureId)).ToList();
        }
        var teamOptions = teams.Select(t => new DirectoryOption(t.TeamId, t.Name)).ToList();
        if (filter.StructureId.HasValue && !structures.Any(s => s.Id == filter.StructureId) || filter.TeamId.HasValue && !teamOptions.Any(t => t.Id == filter.TeamId))
            throw new UnauthorizedAccessException("Select a department or team in your authorised organisational scope.");
        var jobs = await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == organisation && j.JobStateId == 4 && j.JoinedDate <= day &&
                (!j.TerminatedDate.HasValue || j.TerminatedDate > day) && (!filter.StructureId.HasValue || j.OrganisationStructureId == filter.StructureId) &&
                (!filter.JobTypeId.HasValue || j.JobTypeId == filter.JobTypeId) &&
                (!filter.StructureTypeId.HasValue || structures.Select(s => s.Id).Contains(j.OrganisationStructureId)) &&
                (!filter.TeamId.HasValue || db.TeamStaffs.Any(t => t.TeamId == filter.TeamId && t.StaffId == j.IndividualID && t.IsValid && t.StartDate <= day && (!t.EndDate.HasValue || t.EndDate > day))))
            .OrderBy(j => j.IndividualID).Select(j => new { j.JobId, j.IndividualID }).ToListAsync(ct);
        if (orgScope is not null && !orgScope.HasFullOrganisationScope)
        {
            var scopedIds = await OrganisationalAccessScopeResolver.FilterAuthorisedEmployeesAsync(
                db, orgScope, jobs.Select(j => j.IndividualID), day, day, ct);
            jobs = jobs.Where(j => scopedIds.Contains(j.IndividualID)).ToList();
        }
        var types = await db.JobTypes.AsNoTracking().Where(t => t.IsActive != false).OrderBy(t => t.TypeName).Select(t => new DirectoryOption(t.JobTypeId, t.TypeName)).ToListAsync(ct);
        var rows = new List<OrganisationAttendanceStaff>();
        foreach (var job in jobs.DistinctBy(j => j.IndividualID))
        {
            ct.ThrowIfCancellationRequested();
            var person = await db.Individuals.AsNoTracking().Where(p => p.BusinessEntityId == job.IndividualID)
                .Select(p => (p.FirstNameEnglish + " " + (p.MiddleNameEnglish ?? "") + " " + (p.LastNameEnglish ?? "")).Trim()).SingleAsync(ct);
            var employeeNumber = await db.Staffs.Where(s => s.IndividualId == job.IndividualID).Select(s => s.EmployeeNumber).SingleOrDefaultAsync(ct);
            var eligibility = await AttendanceEligibilityCalendar.LoadAsync(db, job.IndividualID, day, day, organisation, job.JobId, ct);
            rows.Add(new(job.IndividualID, job.JobId, person, employeeNumber, eligibility[day],
                await attendance.GetStaffClockEventsAsync(job.IndividualID, day, day, ct), await attendance.GetStaffScheduleAttendanceAsync(job.IndividualID, day, day, ct))
                { Overview = (await attendance.GetStaffAttendanceRangeAsync(job.IndividualID, day, day, ct)).FirstOrDefault() });
        }
        return new(structures, teamOptions, types, rows);
    }
}
