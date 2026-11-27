using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Helpers;

/// <summary>Platform authority requires an explicit, active individual grant, approved portal admission, and an active job.</summary>
public static class PlatformAdministratorAccess
{
    public static Task<bool> HasAsync(HrmTeContext db, int userId)
    {
        var today = DateTime.Today;
        return db.Users.AnyAsync(u => u.UserId == userId &&
            u.IsActive && u.LocalUserStateId == 1 && u.AdminPortalAccessEnabled && u.AdminPortalAccessOrganisationId == u.UserOrganisationId &&
            u.UserOrganisation != null &&
            db.Jobs.Any(j => j.IndividualID == u.BusinessEntityID && j.OrganisationID == u.UserOrganisation.BusinessEntityID &&
                j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= today) &&
            u.UserRoles.Any(r => r.IsActive && r.UserOrganisationId == u.UserOrganisationId && r.Role.RoleKey == "Super.Admin.Manage"));
    }
}
