using HRM.Application.Contracts.Pages;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Queries;

public sealed class OrganisationAttendanceQueries(IDbContextFactory<HrmTeContext> factory,
    IAttendanceService attendance, ApiReadScope scope) : IOrganisationAttendanceQueries
{
    public async Task<OrganisationAttendancePage> GetDailyAsync(OrganisationAttendanceFilter filter, CancellationToken ct = default)
    {
        await scope.RequireStaffReadAsync("Attendance.View");
        var organisation = await scope.OrganisationAsync(filter.OrganisationId);
        var day = ApiReadScope.Dates(filter.Date, filter.Date).From;
        await using var db = await factory.CreateDbContextAsync(ct);
        var structures = await db.OrganisationStructures.AsNoTracking().Where(s => s.OrganisationBusinessEntityID == organisation && s.OrganisationStructureStateId == 3 &&
                (!filter.StructureTypeId.HasValue || s.OrganisationStructureTypeId == filter.StructureTypeId))
            .OrderBy(s => s.Name).Select(s => new DirectoryOption(s.OrganisationStructureId, s.Name)).ToListAsync(ct);
        var teams = await db.Teams.AsNoTracking().Where(t => t.OrganisationId == organisation && t.IsValid == true)
            .OrderBy(t => t.Name).Select(t => new DirectoryOption(t.TeamId, t.Name)).ToListAsync(ct);
        if (filter.StructureId.HasValue && !structures.Any(s => s.Id == filter.StructureId) || filter.TeamId.HasValue && !teams.Any(t => t.Id == filter.TeamId))
            throw new UnauthorizedAccessException("Select a department or team in the current organisation.");
        var jobs = await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == organisation && j.JobStateId == 4 && j.JoinedDate <= day &&
                (!j.TerminatedDate.HasValue || j.TerminatedDate > day) && (!filter.StructureId.HasValue || j.OrganisationStructureId == filter.StructureId) &&
                (!filter.JobTypeId.HasValue || j.JobTypeId == filter.JobTypeId) &&
                (!filter.StructureTypeId.HasValue || structures.Select(s => s.Id).Contains(j.OrganisationStructureId)) &&
                (!filter.TeamId.HasValue || db.TeamStaffs.Any(t => t.TeamId == filter.TeamId && t.StaffId == j.IndividualID && t.IsValid && t.StartDate <= day && (!t.EndDate.HasValue || t.EndDate > day))))
            .OrderBy(j => j.IndividualID).Select(j => new { j.JobId, j.IndividualID }).ToListAsync(ct);
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
                await attendance.GetStaffClockEventsAsync(job.IndividualID, day, day, ct), await attendance.GetStaffScheduleAttendanceAsync(job.IndividualID, day, day, ct)));
        }
        return new(structures, teams, types, rows);
    }
}
