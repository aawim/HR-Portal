using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Helpers;

/// <summary>Platform authority requires an explicit, active grant and approved portal admission.</summary>
public static class PlatformAdministratorAccess
{
    public static Task<bool> HasAsync(HrmTeContext db, int userId) => db.Users.AnyAsync(u => u.UserId == userId &&
        u.IsActive && u.LocalUserStateId == 1 && u.AdminPortalAccessEnabled && u.AdminPortalAccessOrganisationId == u.UserOrganisationId &&
        (u.UserRoles.Any(r => r.IsActive && r.UserOrganisationId == u.UserOrganisationId && r.Role.RoleKey == "Super.Admin.Manage") ||
         u.UserAssignedUserGroups.Any(g => (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisationId == u.UserOrganisationId) &&
             g.UserGroup.UserGroupRoles.Any(r => r.IsActive && r.Role.RoleKey == "Super.Admin.Manage"))));
}
