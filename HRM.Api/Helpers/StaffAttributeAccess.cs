using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

/// <summary>
/// ABAC staff-visibility layer. Callers must complete their normal RBAC and tenant checks first;
/// this policy can only reduce access, never grant an RBAC capability.
/// </summary>
public static class StaffAttributeAccess
{
    public static async Task RequireViewAsync(HrmTeContext db, IUserAccessService access, int individualId, int organisationId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanViewAsync(db, access, individualId, organisationId, cancellationToken))
            throw new UnauthorizedAccessException("This staff record is restricted by the organisation's staff access policy.");
    }

    public static async Task<bool> CanViewAsync(HrmTeContext db, IUserAccessService access, int individualId, int organisationId,
        CancellationToken cancellationToken = default)
    {
        var actor = await access.RequireContextAsync();
        if (actor.IsSuperAdministrator || actor.IndividualId == individualId) return true;
        if (organisationId <= 0) return false;

        var groupIds = await db.UserAssignedUserGroups.AsNoTracking()
            .Where(x => x.UserId == actor.UserId &&
                (x.UserGroup.IsGlobal || x.UserGroup.UserOrganisation!.BusinessEntityID == organisationId))
            .Select(x => x.UserGroupId).ToArrayAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var today = now.AddHours(5).Date;
        var classifications = await db.JobPositions.AsNoTracking()
            .Where(x => x.Job.IndividualID == individualId && x.Job.OrganisationID == organisationId &&
                x.Job.JobStateId == 4 && x.Job.JoinedDate <= today &&
                (!x.Job.TerminatedDate.HasValue || x.Job.TerminatedDate > today) &&
                x.JobPositionStateId == 4 && x.FromDate <= today && (!x.ToDate.HasValue || x.ToDate > today) &&
                x.Position.PositionClassificationId.HasValue)
            .Select(x => x.Position.PositionClassificationId!.Value).Distinct().ToArrayAsync(cancellationToken);

        var matching = db.StaffAccessGrants.AsNoTracking().Where(x => x.OrganisationId == organisationId && x.IsActive &&
            (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > now) &&
            (x.UserId == actor.UserId || x.UserGroupId.HasValue && groupIds.Contains(x.UserGroupId.Value)) &&
            (x.TargetIndividualId == individualId || x.PositionClassificationId.HasValue && classifications.Contains(x.PositionClassificationId.Value)));

        // An explicit deny always wins, including for ordinary classifications.
        if (await matching.AnyAsync(x => !x.IsAllowed, cancellationToken)) return false;
        var explicitlyAllowed = await matching.AnyAsync(x => x.IsAllowed, cancellationToken);
        var restricted = classifications.Length > 0 && await db.StaffAccessPolicies.AsNoTracking().AnyAsync(x =>
            x.OrganisationId == organisationId && x.IsActive && x.IsRestricted && classifications.Contains(x.PositionClassificationId), cancellationToken);
        var explicitOnly = await db.StaffAccessScopes.AsNoTracking().AnyAsync(x => x.OrganisationId == organisationId && x.IsActive &&
            x.RequireExplicitGrant && (x.UserId == actor.UserId || x.UserGroupId.HasValue && groupIds.Contains(x.UserGroupId.Value)), cancellationToken);
        return explicitlyAllowed || (!restricted && !explicitOnly);
    }

    public static async Task<HashSet<int>> FilterViewableAsync(HrmTeContext db, IUserAccessService access, int organisationId,
        IEnumerable<int> individualIds, CancellationToken cancellationToken = default)
    {
        var result = new HashSet<int>();
        foreach (var id in individualIds.Distinct())
            if (await CanViewAsync(db, access, id, organisationId, cancellationToken)) result.Add(id);
        return result;
    }
}
