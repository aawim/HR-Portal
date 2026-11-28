using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

/// <summary>
/// Revokes all roles, direct permissions, access scopes/grants, pending admin grant requests,
/// and Admin/Developer/SuperAdmin portal access when an employee is terminated.
/// </summary>
public static class StaffTerminationAccessRevoker
{
    public static async Task<List<int>> RevokeAllAccessForTerminatedStaffAsync(
        HrmTeContext db,
        int individualId,
        int terminatedJobId,
        int organisationId,
        int? operationLogId,
        int? actorUserId = null,
        string reason = "Employment terminated",
        CancellationToken ct = default)
    {
        if (individualId <= 0) return [];

        var today = DateTime.UtcNow.AddHours(5).Date;
        var hasOtherActiveJob = await db.Jobs.AsNoTracking().AnyAsync(j =>
            j.IndividualID == individualId &&
            j.JobId != terminatedJobId &&
            j.JobStateId == 4 &&
            j.JoinedDate <= today &&
            (!j.TerminatedDate.HasValue || j.TerminatedDate > today), ct);

        var users = await db.Users
            .Where(u => u.BusinessEntityID == individualId)
            .ToListAsync(ct);

        if (users.Count == 0) return [];

        var userIds = users.Select(u => u.UserId).ToList();
        var terminatedUserOrgIds = await db.UserOrganisations.AsNoTracking()
            .Where(uo => uo.BusinessEntityID == organisationId)
            .Select(uo => uo.UserOrganisationID)
            .ToListAsync(ct);

        // 1. Remove assigned role groups (UserAssignedUserGroups)
        var groupAssignments = await db.UserAssignedUserGroups
            .Where(a => userIds.Contains(a.UserId) &&
                        (!hasOtherActiveJob || terminatedUserOrgIds.Contains(a.UserGroup.UserOrganisationId)))
            .ToListAsync(ct);
        if (groupAssignments.Count > 0)
            db.UserAssignedUserGroups.RemoveRange(groupAssignments);

        // 2. Deactivate all direct permissions (UserRoles — includes Admin, Developer, Super.Admin.Manage)
        var directRoles = await db.UserRoles
            .Where(r => userIds.Contains(r.UserId) && r.IsActive &&
                        (!hasOtherActiveJob || terminatedUserOrgIds.Contains(r.UserOrganisationId)))
            .ToListAsync(ct);
        foreach (var r in directRoles)
        {
            r.IsActive = false;
            if (operationLogId.HasValue) r.OperationLogId = operationLogId.Value;
        }

        // 3. Revoke organisational access scopes & staff ABAC scopes/grants
        var orgScopes = await db.UserAccessScopes
            .Where(s => s.UserId.HasValue && userIds.Contains(s.UserId.Value) && s.IsActive &&
                        (!hasOtherActiveJob || s.OrganisationId == organisationId))
            .ToListAsync(ct);
        foreach (var s in orgScopes)
        {
            s.IsActive = false;
            s.RevokedAtUtc = DateTime.UtcNow;
            s.RevokedByUserId = actorUserId;
            s.RevocationReason = reason;
            if (operationLogId.HasValue) s.OperationLogId = operationLogId.Value;
        }

        var abacGrants = await db.StaffAccessGrants
            .Where(g => g.UserId.HasValue && userIds.Contains(g.UserId.Value) && g.IsActive &&
                        (!hasOtherActiveJob || g.OrganisationId == organisationId))
            .ToListAsync(ct);
        foreach (var g in abacGrants)
        {
            g.IsActive = false;
            if (operationLogId.HasValue) g.OperationLogId = operationLogId.Value;
        }

        var abacScopes = await db.StaffAccessScopes
            .Where(s => s.UserId.HasValue && userIds.Contains(s.UserId.Value) && s.IsActive &&
                        (!hasOtherActiveJob || s.OrganisationId == organisationId))
            .ToListAsync(ct);
        foreach (var s in abacScopes)
        {
            s.IsActive = false;
            if (operationLogId.HasValue) s.OperationLogId = operationLogId.Value;
        }

        // 4. Close any open Admin Access Grant requests
        var openGrants = await db.AdminAccessGrantRequests
            .Where(g => userIds.Contains(g.UserId) && g.IsOpen &&
                        (!hasOtherActiveJob || g.OrganisationId == organisationId))
            .ToListAsync(ct);
        foreach (var g in openGrants)
        {
            g.IsOpen = false;
        }

        // 5. Revoke Admin / SuperAdmin portal access flags on Users
        foreach (var u in users)
        {
            if (!hasOtherActiveJob || (u.AdminPortalAccessOrganisationId.HasValue && terminatedUserOrgIds.Contains(u.AdminPortalAccessOrganisationId.Value)))
            {
                u.AdminPortalAccessEnabled = false;
                u.AdminPortalAccessOrganisationId = null;
                if (operationLogId.HasValue) u.OperationLogId = operationLogId.Value;
            }
        }

        return userIds;
    }
}
