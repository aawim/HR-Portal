using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Models;

namespace HRM.AuditLogs.Security;

public sealed record ResolvedAuditQueryScope(
    bool IsGlobalSuperAdmin,
    IReadOnlyCollection<int> AllowedOrganisationIds,
    IReadOnlyCollection<int> AllowedDivisionIds,
    bool IsCrossOrganisationAccess,
    int? RequestedOrganisationId);

public static class AuditAccessPolicy
{
    public static ResolvedAuditQueryScope ResolveQueryScope(AuditActorContext actor, AuditLogFilter filter)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(filter);

        if (!actor.IsSuperAdministrator && !actor.CanViewAuditLogs)
            throw new UnauthorizedAccessException("Audit log viewing permission is required.");

        if (actor.IsSuperAdministrator)
        {
            var isCrossOrg = filter.OrganisationId.HasValue && actor.OrganisationId.HasValue &&
                             filter.OrganisationId.Value != actor.OrganisationId.Value;
            return new ResolvedAuditQueryScope(
                IsGlobalSuperAdmin: true,
                AllowedOrganisationIds: filter.OrganisationId.HasValue ? [filter.OrganisationId.Value] : Array.Empty<int>(),
                AllowedDivisionIds: filter.DivisionId.HasValue ? [filter.DivisionId.Value] : Array.Empty<int>(),
                IsCrossOrganisationAccess: isCrossOrg || !filter.OrganisationId.HasValue,
                RequestedOrganisationId: filter.OrganisationId);
        }

        var authorizedOrgs = new HashSet<int>(actor.AuthorizedOrganisationIds.Where(id => id > 0));
        if (actor.OrganisationId.HasValue && actor.OrganisationId.Value > 0)
            authorizedOrgs.Add(actor.OrganisationId.Value);

        if (authorizedOrgs.Count == 0)
            throw new UnauthorizedAccessException("No authorised organisation scope is available for audit queries.");

        var primaryOrg = actor.OrganisationId ?? authorizedOrgs.First();
        var isCrossOrgRequest = false;
        IReadOnlyCollection<int> effectiveOrgs;

        if (filter.OrganisationId.HasValue && filter.OrganisationId.Value > 0)
        {
            var requestedOrg = filter.OrganisationId.Value;
            if (requestedOrg != primaryOrg)
            {
                if (!actor.CanViewCrossOrganisationAudit || !authorizedOrgs.Contains(requestedOrg))
                    throw new UnauthorizedAccessException("Cross-organisation audit access requires explicit delegated authorisation.");
                isCrossOrgRequest = true;
            }
            else if (!authorizedOrgs.Contains(requestedOrg))
            {
                throw new UnauthorizedAccessException("You do not have audit access to the requested organisation.");
            }
            effectiveOrgs = [requestedOrg];
        }
        else
        {
            // Ordinary tenant users never get unrestricted global searches; default strictly to their active organisation.
            effectiveOrgs = [primaryOrg];
        }

        var authorizedDivisions = new HashSet<int>(actor.AuthorizedDivisionIds.Where(id => id > 0));
        IReadOnlyCollection<int> effectiveDivisions = Array.Empty<int>();
        if (authorizedDivisions.Count > 0)
        {
            if (filter.DivisionId.HasValue && filter.DivisionId.Value > 0)
            {
                if (!authorizedDivisions.Contains(filter.DivisionId.Value))
                    throw new UnauthorizedAccessException("You do not have audit access to the requested division.");
                effectiveDivisions = [filter.DivisionId.Value];
            }
            else
            {
                effectiveDivisions = authorizedDivisions.ToArray();
            }
        }
        else if (filter.DivisionId.HasValue && filter.DivisionId.Value > 0)
        {
            effectiveDivisions = [filter.DivisionId.Value];
        }

        return new ResolvedAuditQueryScope(
            IsGlobalSuperAdmin: false,
            AllowedOrganisationIds: effectiveOrgs,
            AllowedDivisionIds: effectiveDivisions,
            IsCrossOrganisationAccess: isCrossOrgRequest,
            RequestedOrganisationId: filter.OrganisationId);
    }

    public static void EnsureCanExport(AuditActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!actor.IsSuperAdministrator && !actor.CanExportAuditLogs)
            throw new UnauthorizedAccessException("Audit log export permission is required.");
    }

    public static void EnsureCanReadRecord(AuditLog record, AuditActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(actor);

        if (!actor.IsSuperAdministrator && !actor.CanViewAuditLogs)
            throw new UnauthorizedAccessException("Audit log viewing permission is required.");

        if (actor.IsSuperAdministrator) return;

        var authorizedOrgs = new HashSet<int>(actor.AuthorizedOrganisationIds.Where(id => id > 0));
        if (actor.OrganisationId.HasValue && actor.OrganisationId.Value > 0)
            authorizedOrgs.Add(actor.OrganisationId.Value);

        if (!record.OrganisationId.HasValue)
            throw new UnauthorizedAccessException("Global system audit records are restricted to system administrators.");

        if (!authorizedOrgs.Contains(record.OrganisationId.Value))
            throw new UnauthorizedAccessException("This audit record belongs to another organisation.");

        if (actor.OrganisationId.HasValue && record.OrganisationId.Value != actor.OrganisationId.Value && !actor.CanViewCrossOrganisationAudit)
            throw new UnauthorizedAccessException("Cross-organisation audit access requires explicit delegated authorisation.");

        if (actor.AuthorizedDivisionIds.Count > 0)
        {
            if (!record.DivisionId.HasValue || !actor.AuthorizedDivisionIds.Contains(record.DivisionId.Value))
                throw new UnauthorizedAccessException("This audit record is outside your authorised division scope.");
        }
    }
}
