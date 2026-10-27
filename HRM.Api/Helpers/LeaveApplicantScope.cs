using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

/// <summary>Resolves the employee without replacing the authenticated requester.</summary>
public static class LeaveApplicantScope
{
    public static async Task<int> RequireHrOrganisationAsync(IUserAccessService access)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) ||
            !(actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager ||
              actor.IsOrganisationAdmin || await access.HasPermissionAsync("Admin")))
            throw new UnauthorizedAccessException("Only authorised HR staff or administrators can apply on behalf of staff in the current organisation.");
        return org;
    }

    public static async Task<Job> ResolveAsync(HrmTeContext db, IUserAccessService access, int? applicantJobId = null)
    {
        var actor = await access.RequireContextAsync();
        var jobId = applicantJobId ?? actor.ActiveJob?.JobId ?? 0;
        var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(j => j.JobId == jobId)
            ?? throw new InvalidOperationException("An active employment record is required to apply for leave.");
        if (applicantJobId.HasValue)
        {
            var org = await RequireHrOrganisationAsync(access);
            var today = DateTime.UtcNow.AddHours(5).Date;
            if (job.OrganisationID != org || job.JobStateId != 4 || job.JoinedDate.Date > today ||
                job.TerminatedDate.HasValue && job.TerminatedDate.Value.Date <= today)
                throw new UnauthorizedAccessException("Select an active staff employment record in the current organisation.");
        }
        else if (job.IndividualID != actor.IndividualId)
            throw new UnauthorizedAccessException("The employee session does not match this employment record.");
        return job;
    }
}
