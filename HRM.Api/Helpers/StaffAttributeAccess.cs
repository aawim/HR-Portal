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

        // Enforce hierarchical Organisational Access Scopes (Division / Department / Team / Custom / Self isolation).
        var inOrgScope = await OrganisationalAccessScopeResolver.CanViewEmployeeAsync(
            db, access, individualId, organisationId, ct: cancellationToken);
        if (!inOrgScope && !explicitlyAllowed)
            return false;

        var restricted = classifications.Length > 0 && await db.StaffAccessPolicies.AsNoTracking().AnyAsync(x =>
            x.OrganisationId == organisationId && x.IsActive && x.IsRestricted && classifications.Contains(x.PositionClassificationId), cancellationToken);
        var explicitOnly = await db.StaffAccessScopes.AsNoTracking().AnyAsync(x => x.OrganisationId == organisationId && x.IsActive &&
            x.RequireExplicitGrant && (x.UserId == actor.UserId || x.UserGroupId.HasValue && groupIds.Contains(x.UserGroupId.Value)), cancellationToken);
        return explicitlyAllowed || (!restricted && !explicitOnly);
    }

    public static async Task<HashSet<int>> FilterViewableAsync(HrmTeContext db, IUserAccessService access, int organisationId,
        IEnumerable<int> individualIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = individualIds.Distinct().ToArray();
        if (distinctIds.Length == 0) return [];

        var actor = await access.RequireContextAsync();
        if (actor.IsSuperAdministrator) return [.. distinctIds];
        if (organisationId <= 0)
            return distinctIds.Contains(actor.IndividualId) ? [actor.IndividualId] : [];

        var orgScope = await OrganisationalAccessScopeResolver.ResolveCurrentUserAsync(
            db, access, organisationId, ct: cancellationToken);
        var scopedIds = await OrganisationalAccessScopeResolver.FilterAuthorisedEmployeesAsync(
            db, orgScope, distinctIds, ct: cancellationToken);

        var groupIds = await db.UserAssignedUserGroups.AsNoTracking()
            .Where(x => x.UserId == actor.UserId &&
                (x.UserGroup.IsGlobal || x.UserGroup.UserOrganisation!.BusinessEntityID == organisationId))
            .Select(x => x.UserGroupId)
            .ToArrayAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = now.AddHours(5).Date;

        var grants = await db.StaffAccessGrants.AsNoTracking()
            .Where(x => x.OrganisationId == organisationId && x.IsActive &&
                (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc > now) &&
                (x.UserId == actor.UserId || (x.UserGroupId.HasValue && groupIds.Contains(x.UserGroupId.Value))))
            .Select(x => new { x.TargetIndividualId, x.PositionClassificationId, x.IsAllowed })
            .ToListAsync(cancellationToken);

        var restrictedClassifications = (await db.StaffAccessPolicies.AsNoTracking()
            .Where(x => x.OrganisationId == organisationId && x.IsActive && x.IsRestricted)
            .Select(x => x.PositionClassificationId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var explicitOnly = await db.StaffAccessScopes.AsNoTracking()
            .AnyAsync(x => x.OrganisationId == organisationId && x.IsActive && x.RequireExplicitGrant &&
                (x.UserId == actor.UserId || (x.UserGroupId.HasValue && groupIds.Contains(x.UserGroupId.Value))), cancellationToken);

        var needClassifications = restrictedClassifications.Count > 0 || grants.Any(g => g.PositionClassificationId.HasValue);
        var classificationsByIndividual = new Dictionary<int, HashSet<int>>();
        if (needClassifications)
        {
            var jobClassifications = await db.JobPositions.AsNoTracking()
                .Where(x => distinctIds.Contains(x.Job.IndividualID) && x.Job.OrganisationID == organisationId &&
                    x.Job.JobStateId == 4 && x.Job.JoinedDate <= today &&
                    (!x.Job.TerminatedDate.HasValue || x.Job.TerminatedDate > today) &&
                    x.JobPositionStateId == 4 && x.FromDate <= today && (!x.ToDate.HasValue || x.ToDate > today) &&
                    x.Position.PositionClassificationId.HasValue)
                .Select(x => new { x.Job.IndividualID, ClassificationId = x.Position.PositionClassificationId!.Value })
                .ToListAsync(cancellationToken);

            foreach (var row in jobClassifications)
            {
                if (!classificationsByIndividual.TryGetValue(row.IndividualID, out var set))
                {
                    set = [];
                    classificationsByIndividual[row.IndividualID] = set;
                }
                set.Add(row.ClassificationId);
            }
        }

        var deniedIndividuals = grants.Where(g => !g.IsAllowed && g.TargetIndividualId.HasValue).Select(g => g.TargetIndividualId!.Value).ToHashSet();
        var allowedIndividuals = grants.Where(g => g.IsAllowed && g.TargetIndividualId.HasValue).Select(g => g.TargetIndividualId!.Value).ToHashSet();
        var deniedClassifications = grants.Where(g => !g.IsAllowed && g.PositionClassificationId.HasValue).Select(g => g.PositionClassificationId!.Value).ToHashSet();
        var allowedClassifications = grants.Where(g => g.IsAllowed && g.PositionClassificationId.HasValue).Select(g => g.PositionClassificationId!.Value).ToHashSet();

        var result = new HashSet<int>();
        foreach (var id in distinctIds)
        {
            if (id == actor.IndividualId)
            {
                result.Add(id);
                continue;
            }

            var inOrgScope = scopedIds.Contains(id);
            if (!inOrgScope && !orgScope.HasOrganisationTenantAccess) continue;

            classificationsByIndividual.TryGetValue(id, out var empClassifications);

            var explicitlyDenied = deniedIndividuals.Contains(id) ||
                (empClassifications != null && empClassifications.Any(deniedClassifications.Contains));
            if (explicitlyDenied) continue;

            var explicitlyAllowed = allowedIndividuals.Contains(id) ||
                (empClassifications != null && empClassifications.Any(allowedClassifications.Contains));
            if (!inOrgScope && !explicitlyAllowed) continue;

            var restricted = empClassifications != null && empClassifications.Any(restrictedClassifications.Contains);
            if (explicitlyAllowed || (!restricted && !explicitOnly))
                result.Add(id);
        }

        return result;
    }
}
