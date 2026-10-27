using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

// Callers authorise the action and own the transaction. This writer never enables portal access.
internal static class UserAccessAssignmentWriter
{
    internal static async Task ApplyAsync(HrmTeContext db, User user, IEnumerable<int> groupIds, IEnumerable<int> permissionIds, int auditId)
    {
        if (user.UserOrganisationId is not int orgId) throw new InvalidOperationException("The account needs an organisation.");
        var groups = groupIds.Distinct().ToArray(); var permissions = permissionIds.Distinct().ToArray();
        if (await db.UserGroups.CountAsync(g => groups.Contains(g.UserGroupId) && g.UserOrganisationId == orgId) != groups.Length)
            throw new InvalidOperationException("Select User Groups from the account's organisation.");
        if (await db.UserOrganisationRoles.Where(p => p.UserOrganisationID == orgId && p.IsActive && permissions.Contains(p.RoleID)).Select(p => p.RoleID).Distinct().CountAsync() != permissions.Length)
            throw new InvalidOperationException("Some permissions are not enabled for the account's organisation.");
        await db.Entry(user).Collection(u => u.UserRoles).LoadAsync();
        await db.Entry(user).Collection(u => u.UserAssignedUserGroups).Query().Include(g => g.UserGroup).LoadAsync();
        foreach (var link in user.UserRoles.Where(p => p.UserOrganisationId == orgId))
        {
            link.IsActive = permissions.Contains(link.RoleId); link.OperationLogId = auditId;
        }
        foreach (var id in permissions.Except(user.UserRoles.Where(p => p.UserOrganisationId == orgId).Select(p => p.RoleId)))
            user.UserRoles.Add(new() { RoleId = id, UserOrganisationId = orgId, IsActive = true, OperationLogId = auditId });
        foreach (var link in user.UserAssignedUserGroups.Where(g => g.UserGroup.UserOrganisationId == orgId && !groups.Contains(g.UserGroupId)).ToList())
            db.UserAssignedUserGroups.Remove(link);
        foreach (var id in groups.Except(user.UserAssignedUserGroups.Select(g => g.UserGroupId)))
            user.UserAssignedUserGroups.Add(new() { UserGroupId = id, OperationLogId = auditId });
    }
}
