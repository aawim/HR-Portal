using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;

namespace HRM.ActivityLogs.Security;

public sealed record ResolvedActivityScope(
    bool IsGlobalSuperAdmin,
    bool SelfOnly,
    int? SelfUserId,
    IReadOnlyCollection<int> AllowedOrganisationIds,
    IReadOnlyCollection<int> AllowedDivisionIds,
    IReadOnlyCollection<int> AllowedTeamIds,
    bool IsCrossOrganisationAccess,
    int? RequestedOrganisationId);

public static class ActivityAccessPolicy
{
    public static ResolvedActivityScope ResolveSearchScope(ActivityActorContext actor, ActivityLogFilter filter)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(filter);

        if (actor.IsSuperAdministrator)
        {
            var isCross = filter.OrganisationId.HasValue && actor.OrganisationId.HasValue &&
                          filter.OrganisationId.Value != actor.OrganisationId.Value;
            return new ResolvedActivityScope(
                IsGlobalSuperAdmin: true,
                SelfOnly: false,
                SelfUserId: null,
                AllowedOrganisationIds: filter.OrganisationId.HasValue ? [filter.OrganisationId.Value] : Array.Empty<int>(),
                AllowedDivisionIds: filter.DivisionId.HasValue ? [filter.DivisionId.Value] : Array.Empty<int>(),
                AllowedTeamIds: filter.TeamId.HasValue ? [filter.TeamId.Value] : Array.Empty<int>(),
                IsCrossOrganisationAccess: isCross,
                RequestedOrganisationId: filter.OrganisationId);
        }

        if (!actor.CanViewOrganisationActivity && !actor.CanViewTeamActivity)
        {
            if (!actor.UserId.HasValue || actor.UserId.Value <= 0)
                throw new UnauthorizedAccessException("Activity log viewing permission is required.");

            // Regular user can only view their own activity within their active organisation.
            if (filter.UserId.HasValue && filter.UserId.Value != actor.UserId.Value)
                throw new UnauthorizedAccessException("You can only view your own activity timeline.");

            var selfOrg = actor.OrganisationId ?? actor.AuthorizedOrganisationIds.FirstOrDefault();
            if (filter.OrganisationId.HasValue && selfOrg > 0 && filter.OrganisationId.Value != selfOrg)
                throw new UnauthorizedAccessException("Cross-organisation activity access is not permitted.");

            return new ResolvedActivityScope(
                IsGlobalSuperAdmin: false,
                SelfOnly: true,
                SelfUserId: actor.UserId.Value,
                AllowedOrganisationIds: selfOrg > 0 ? [selfOrg] : Array.Empty<int>(),
                AllowedDivisionIds: Array.Empty<int>(),
                AllowedTeamIds: Array.Empty<int>(),
                IsCrossOrganisationAccess: false,
                RequestedOrganisationId: filter.OrganisationId);
        }

        var authorizedOrgs = new HashSet<int>(actor.AuthorizedOrganisationIds.Where(id => id > 0));
        if (actor.OrganisationId.HasValue && actor.OrganisationId.Value > 0)
            authorizedOrgs.Add(actor.OrganisationId.Value);

        if (authorizedOrgs.Count == 0)
            throw new UnauthorizedAccessException("No authorised organisation scope is available for activity queries.");

        var primaryOrg = actor.OrganisationId ?? authorizedOrgs.First();
        var isCrossOrg = false;
        IReadOnlyCollection<int> effectiveOrgs;

        if (filter.OrganisationId.HasValue && filter.OrganisationId.Value > 0)
        {
            var reqOrg = filter.OrganisationId.Value;
            if (reqOrg != primaryOrg)
            {
                if (!actor.CanViewCrossOrganisationActivity || !authorizedOrgs.Contains(reqOrg))
                    throw new UnauthorizedAccessException("Cross-organisation activity access requires explicit authorisation.");
                isCrossOrg = true;
            }
            else if (!authorizedOrgs.Contains(reqOrg))
            {
                throw new UnauthorizedAccessException("You do not have activity access to the requested organisation.");
            }
            effectiveOrgs = [reqOrg];
        }
        else
        {
            effectiveOrgs = [primaryOrg];
        }

        // Division isolation
        var authorizedDivs = new HashSet<int>(actor.AuthorizedDivisionIds.Where(id => id > 0));
        IReadOnlyCollection<int> effectiveDivs = Array.Empty<int>();
        if (authorizedDivs.Count > 0)
        {
            if (filter.DivisionId.HasValue && filter.DivisionId.Value > 0)
            {
                if (!authorizedDivs.Contains(filter.DivisionId.Value))
                    throw new UnauthorizedAccessException("Requested division is outside your authorised scope.");
                effectiveDivs = [filter.DivisionId.Value];
            }
            else
            {
                effectiveDivs = authorizedDivs.ToArray();
            }
        }
        else if (filter.DivisionId.HasValue && filter.DivisionId.Value > 0)
        {
            effectiveDivs = [filter.DivisionId.Value];
        }

        // Team isolation (when user only has team-level activity access, restrict strictly to authorised teams)
        var authorizedTeams = new HashSet<int>(actor.AuthorizedTeamIds.Where(id => id > 0));
        IReadOnlyCollection<int> effectiveTeams = Array.Empty<int>();
        if (!actor.CanViewOrganisationActivity && actor.CanViewTeamActivity)
        {
            if (authorizedTeams.Count == 0)
                throw new UnauthorizedAccessException("No authorised team scope is available for team activity queries.");

            if (filter.TeamId.HasValue && filter.TeamId.Value > 0)
            {
                if (!authorizedTeams.Contains(filter.TeamId.Value))
                    throw new UnauthorizedAccessException("Requested team is outside your authorised scope.");
                effectiveTeams = [filter.TeamId.Value];
            }
            else
            {
                effectiveTeams = authorizedTeams.ToArray();
            }
        }
        else if (filter.TeamId.HasValue && filter.TeamId.Value > 0)
        {
            effectiveTeams = [filter.TeamId.Value];
        }

        return new ResolvedActivityScope(
            IsGlobalSuperAdmin: false,
            SelfOnly: false,
            SelfUserId: null,
            AllowedOrganisationIds: effectiveOrgs,
            AllowedDivisionIds: effectiveDivs,
            AllowedTeamIds: effectiveTeams,
            IsCrossOrganisationAccess: isCrossOrg,
            RequestedOrganisationId: filter.OrganisationId);
    }

    public static void EnsureCanExport(ActivityActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!actor.IsSuperAdministrator && !actor.CanExportActivity)
            throw new UnauthorizedAccessException("Activity log export permission is required.");
    }

    public static void EnsureCanReadRecord(ActivityLog record, ActivityActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(actor);

        if (actor.IsSuperAdministrator) return;

        if (actor.UserId.HasValue && record.UserId == actor.UserId.Value &&
            (!actor.OrganisationId.HasValue || !record.OrganisationId.HasValue || record.OrganisationId == actor.OrganisationId))
            return;

        if (!actor.CanViewOrganisationActivity && !actor.CanViewTeamActivity)
            throw new UnauthorizedAccessException("You can only view your own activity records.");

        var authorizedOrgs = new HashSet<int>(actor.AuthorizedOrganisationIds.Where(id => id > 0));
        if (actor.OrganisationId.HasValue && actor.OrganisationId.Value > 0)
            authorizedOrgs.Add(actor.OrganisationId.Value);

        if (!record.OrganisationId.HasValue || !authorizedOrgs.Contains(record.OrganisationId.Value))
            throw new UnauthorizedAccessException("This activity record belongs to another organisation.");

        if (actor.OrganisationId.HasValue && record.OrganisationId.Value != actor.OrganisationId.Value && !actor.CanViewCrossOrganisationActivity)
            throw new UnauthorizedAccessException("Cross-organisation activity access requires explicit authorisation.");

        if (actor.AuthorizedDivisionIds.Count > 0 && (!record.DivisionId.HasValue || !actor.AuthorizedDivisionIds.Contains(record.DivisionId.Value)))
            throw new UnauthorizedAccessException("This activity record is outside your authorised division scope.");

        if (!actor.CanViewOrganisationActivity && actor.CanViewTeamActivity &&
            (!record.TeamId.HasValue || !actor.AuthorizedTeamIds.Contains(record.TeamId.Value)))
            throw new UnauthorizedAccessException("This activity record is outside your authorised team scope.");
    }
}
