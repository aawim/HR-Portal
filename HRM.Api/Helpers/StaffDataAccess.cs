using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class StaffDataAccess
{
    public static async Task RequireJobAsync(HrmTeContext db, IUserAccessService access, int jobId)
    {
        var job = await db.Jobs.AsNoTracking().Where(j => j.JobId == jobId)
            .Select(j => new { j.IndividualID, j.OrganisationID }).SingleOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("Employment record is unavailable.");
        var scope = await RequireIndividualAsync(db, access, job.IndividualID);
        if (scope.HasValue && scope.Value != job.OrganisationID)
            throw new UnauthorizedAccessException("Employment record is outside the current organisation.");
    }

    // Own history is visible across employments; staff administration stays in the selected tenant.
    public static async Task<int?> RequireIndividualAsync(HrmTeContext db, IUserAccessService access, int individualId)
    {
        var actor = await access.RequireContextAsync();
        if (individualId > 0 && actor.IndividualId == individualId) return null;
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) ||
            !(actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin ||
              await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Employee.View")) ||
            !await db.Jobs.AnyAsync(j => j.IndividualID == individualId && j.OrganisationID == org))
            throw new UnauthorizedAccessException("Staff information is unavailable in the current organisation.");
        return org;
    }
}
