using HRM.DTOs.Attendance;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class WeeklyWorkingHoursService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
{
    public async Task<WeeklyWorkedHoursDto> LoadAsync(int person, DateTime date)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select an organisation you can access.");
        await using var db = await factory.CreateDbContextAsync();
        if (person <= 0) person = actor.IndividualId;
        var jobs = await db.Jobs.Where(j => j.IndividualID == person && j.OrganisationID == org).Select(j => j.JobId).ToListAsync();
        if (jobs.Count == 0) throw new UnauthorizedAccessException("The employee is not in this organisation.");
        if (person != actor.IndividualId && !actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") &&
            !await access.HasPermissionAsync(WorkingHoursSettingsService.ReviewPermission))
        {
            var supervisor = false;
            foreach (var job in jobs)
                if ((await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db, job)).Contains(actor.IndividualId)) { supervisor = true; break; }
            if (!supervisor) throw new UnauthorizedAccessException("Only the employee's supervisor or authorised HR staff can review weekly hours.");
        }
        if (date.Year < 2000 || date.Date > DateTime.UtcNow.AddHours(5).Date.AddYears(1)) throw new InvalidOperationException("Select a valid week.");
        return await WeeklyWorkingHours.LoadAsync(db, person, org, date);
    }
}
