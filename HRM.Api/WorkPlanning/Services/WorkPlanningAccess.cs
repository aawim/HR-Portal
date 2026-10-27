using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

internal static class WorkPlanningAccess
{
    public static async Task<int> RequireOrganisationAsync(IUserAccessService access, bool write = false)
    {
        await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || (write && !await access.HasPermissionAsync("Schedule.Attach")))
            throw new UnauthorizedAccessException(write ? "Schedule.Attach permission and access to the current organisation are required." : "Access to the current organisation is required.");
        return org;
    }

    public static async Task RequireTemplateAsync(HrmTeContext db, IUserAccessService access, int templateId, bool write = false)
    {
        var org = await RequireOrganisationAsync(access, write);
        var template = await db.WorkTemplates.AsNoTracking().Where(x => x.WorkTemplateId == templateId)
            .Select(x => new { x.IsGlobal, x.OrganisationBusinessEntityId }).SingleOrDefaultAsync();
        if (template == null || (!template.IsGlobal && template.OrganisationBusinessEntityId != org) ||
            (write && template.IsGlobal && !await access.HasUserGroupAsync("Developer")))
            throw new UnauthorizedAccessException("This template is unavailable in the current organisation. Global template changes require the Developer user group.");
    }

    public static async Task RequireJobAsync(HrmTeContext db, IUserAccessService access, int jobId)
    {
        var org = await RequireOrganisationAsync(access);
        if (!await db.Jobs.AnyAsync(x => x.JobId == jobId && x.OrganisationID == org))
            throw new UnauthorizedAccessException("The staff job is outside the current organisation.");
    }
}
