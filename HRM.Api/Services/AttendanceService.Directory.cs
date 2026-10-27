using HRM.DTOs.Attendance;
using HRM.DTOs.UserContext;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class AttendanceService
{
    public async Task<List<YearlyAttendanceDayDto>> GetStaffAttendanceRangeAsync(int staffId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var org = await RequireStaffAttendanceAccessAsync(staffId, cancellationToken);
        if (from > to || from.Year < 2000 || (to.Date - from.Date).Days > 366)
            throw new InvalidOperationException("Choose a date range of up to one year.");
        var today = DateTime.UtcNow.AddHours(5).Date;
        var end = to.Date > today ? today : to.Date;
        var result = new List<YearlyAttendanceDayDto>();
        for (var year = from.Year; year <= end.Year; year++)
            result.AddRange(await GetYearlyAttendanceAsync(staffId, year, org, cancellationToken,
                from.Year == year ? from.Date : new DateTime(year, 1, 1),
                end.Year == year ? end : new DateTime(year, 12, 31)));
        return result;
    }
    public async Task<List<StaffScheduleAttendanceDto>> GetStaffScheduleAttendanceAsync(int staffId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var org = await RequireStaffAttendanceAccessAsync(staffId, cancellationToken);
        if (from > to || (to.Date - from.Date).Days > 366) throw new InvalidOperationException("Choose a date range of up to one year.");
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await HRM.Helpers.AttendanceScheduleFacts.LoadAsync(db, staffId, from, to, org, cancellationToken);
    }
    private async Task<bool> CanViewAllStaffAttendanceAsync(UserContextDto actor) => actor.IsHRStaff || actor.IsHRHead ||
        actor.IsHRManager || actor.IsSuperAdministrator || await _userAccessService.HasPermissionAsync("Super.Admin.Manage") ||
        await _userAccessService.HasPermissionAsync("Admin");

    public async Task<AttendanceDirectoryDto> GetAttendanceDirectoryAsync(CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.RequireContextAsync();
        var org = await _userAccessService.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await _userAccessService.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("Select an organisation you can access.");
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.UtcNow.AddHours(5).Date;
        var jobs = db.Jobs.AsNoTracking().Where(j => j.OrganisationID == org && j.JobStateId == 4 &&
            j.JoinedDate <= today && j.TerminatedDate == null);
        var teams = db.Teams.AsNoTracking().Where(t => t.OrganisationId == org && t.IsValid == true &&
            (t.StartDate == null || t.StartDate <= today) && (t.EndDate == null || t.EndDate >= today));
        var memberships = db.TeamStaffs.AsNoTracking().Where(m => m.IsValid && m.StartDate <= today &&
            (m.EndDate == null || m.EndDate >= today) && teams.Any(t => t.TeamId == m.TeamId));
        if (!await CanViewAllStaffAttendanceAsync(actor))
        {
            var supervisor = await jobs.AnyAsync(j => j.IndividualID == actor.IndividualId, cancellationToken);
            var departments = db.OrganisationStructureSupervisors.Where(s => s.SupervisorStaffId == actor.IndividualId && s.IsActive && s.IsValid)
                .Select(s => s.OrganisationStructureId);
            var supervisedTeams = memberships.Where(m => m.StaffId == actor.IndividualId && m.IsSuperVisor).Select(m => m.TeamId);
            jobs = jobs.Where(j => j.IndividualID == actor.IndividualId || supervisor &&
                (departments.Contains(j.OrganisationStructureId) || memberships.Any(m => m.StaffId == j.IndividualID && supervisedTeams.Contains(m.TeamId))));
        }
        var people = await jobs.Select(j => new { j.IndividualID, j.OrganisationStructureId,
            Name = (j.Individual.FirstNameEnglish + " " + (j.Individual.MiddleNameEnglish ?? "") + " " + (j.Individual.LastNameEnglish ?? "")).Trim() })
            .ToListAsync(cancellationToken);
        var ids = people.Select(p => p.IndividualID).Distinct().ToArray();
        var links = await memberships.Where(m => ids.Contains(m.StaffId)).Select(m => new { m.StaffId, m.TeamId }).ToListAsync(cancellationToken);
        var departmentIds = people.Select(p => p.OrganisationStructureId).Distinct().ToArray();
        var teamIds = links.Select(m => m.TeamId).Distinct().ToArray();
        return new AttendanceDirectoryDto
        {
            Organisation = await db.Organisations.Where(o => o.BusinessEntityID == org).Select(o => o.OrganisationName).SingleAsync(cancellationToken),
            Departments = await db.OrganisationStructures.Where(d => d.OrganisationBusinessEntityID == org && d.OrganisationStructureStateId == 3 && departmentIds.Contains(d.OrganisationStructureId))
                .OrderBy(d => d.Name).Select(d => new AttendanceDirectoryOption(d.OrganisationStructureId, d.Name, d.OrganisationStructureTypeId)).ToListAsync(cancellationToken),
            DepartmentTypes = await db.OrganisationStructureTypes.OrderBy(t => t.TypeName)
                .Select(t => new AttendanceDirectoryOption(t.OrganisationStructureTypeId, t.TypeName, null)).ToListAsync(cancellationToken),
            Teams = await teams.Where(t => teamIds.Contains(t.TeamId)).OrderBy(t => t.Name)
                .Select(t => new AttendanceDirectoryOption(t.TeamId, t.Name, null)).ToListAsync(cancellationToken),
            Staff = people.GroupBy(p => p.IndividualID).Select(g => new AttendanceDirectoryStaff(g.Key, g.First().Name,
                g.Select(p => p.OrganisationStructureId).Distinct().ToList(), links.Where(m => m.StaffId == g.Key).Select(m => m.TeamId).Distinct().ToList()))
                .OrderBy(p => p.Name).ToList()
        };
    }
}
