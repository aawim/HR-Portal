using HRM.Authorization;
using HRM.Components.Shared;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public sealed class DataClassificationAccess
{
    public bool CanViewPublicProfile { get; init; } = true;
    public bool CanViewBasicEmployment { get; init; }
    public bool CanViewPrivateRecord { get; init; }
    public bool CanViewSalaryAndBank { get; init; }
    public bool CanViewIdentification { get; init; }
    public bool CanViewDisciplinary { get; init; }
    public bool CanViewMedical { get; init; }
    public bool CanViewIntranetAnnouncements { get; init; } = true;
}

public sealed class ResolvedOrganisationalAccess
{
    public int UserId { get; init; }
    public int IndividualId { get; init; }
    public int OrganisationId { get; init; }
    public bool IsSuperAdministrator { get; init; }
    public bool HasOrganisationTenantAccess { get; init; }
    public bool HasFullOrganisationScope { get; init; }
    public bool HasExplicitScopeGrants { get; init; }
    public HashSet<int> AuthorisedStructureIds { get; init; } = [];
    public HashSet<int> AuthorisedTeamIds { get; init; } = [];
    public List<UserAccessScope> ActiveGrants { get; init; } = [];
    public DataClassificationAccess FieldPermissions { get; init; } = new();

    public bool CanAccessUnit(int? structureId)
    {
        if (!HasOrganisationTenantAccess) return false;
        if (HasFullOrganisationScope) return true;
        return structureId.HasValue && AuthorisedStructureIds.Contains(structureId.Value);
    }

    public bool CanAccessTeam(int? teamId, int? teamStructureId = null)
    {
        if (!HasOrganisationTenantAccess) return false;
        if (HasFullOrganisationScope) return true;
        if (teamId.HasValue && AuthorisedTeamIds.Contains(teamId.Value)) return true;
        return teamStructureId.HasValue && AuthorisedStructureIds.Contains(teamStructureId.Value);
    }
}

/// <summary>
/// Centralised Organisational Data Isolation and Access Control Framework resolver.
/// Combines RBAC permissions, hierarchical Organisational Access Scopes (Self, Team, Department,
/// Division, DivisionAndDescendants, Organisation, Custom), effective/historical job assignments,
/// and field-level data classification rules.
/// </summary>
public static class OrganisationalAccessScopeResolver
{
    private static int _schemaInitialized;
    public static DateTime TodayMaldives => DateTime.UtcNow.AddHours(5).Date;

    public static async Task EnsureSchemaAsync(IServiceProvider services)
    {
        try
        {
            var factory = services.GetService<IDbContextFactory<HrmTeContext>>();
            if (factory is null) return;
            await using var db = await factory.CreateDbContextAsync();
            await EnsureSchemaAsync(db);
        }
        catch
        {
            // Ignored on startup if database is not reachable yet
        }
    }

    public static async Task EnsureSchemaAsync(HrmTeContext db, CancellationToken ct = default)
    {
        if (!db.Database.IsSqlServer()) return;
        if (Interlocked.Exchange(ref _schemaInitialized, 1) == 1) return;

        try
        {
            using var stream = typeof(OrganisationalAccessScopeResolver).Assembly
                .GetManifestResourceStream("HRM.Database.Scripts.20261115_OrganisationalAccessScopeFramework.sql");
            if (stream is null) return;

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await db.Database.ExecuteSqlRawAsync(sql, ct);
            }
        }
        catch
        {
            Interlocked.Exchange(ref _schemaInitialized, 0);
        }
    }

    public static async Task<ResolvedOrganisationalAccess> ResolveCurrentUserAsync(
        HrmTeContext db,
        IUserAccessService access,
        int organisationId,
        string? permissionFilter = null,
        CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var hasTenantAccess = actor.IsSuperAdministrator || await access.HasOrganisationAccessAsync(organisationId);

        if (!hasTenantAccess && actor.IndividualId > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            hasTenantAccess = await (
                from a in db.LeadershipAppointments.AsNoTracking()
                join o in db.LeadershipAppointmentOrganisations.AsNoTracking()
                    on a.LeadershipAppointmentId equals o.LeadershipAppointmentId
                where a.IndividualId == actor.IndividualId &&
                      a.Status == "Active" &&
                      a.EffectiveFrom <= today && (!a.EffectiveTo.HasValue || a.EffectiveTo >= today) &&
                      o.OrganisationId == organisationId &&
                      o.EffectiveFrom <= today && (!o.EffectiveTo.HasValue || o.EffectiveTo >= today)
                select o.LeadershipAppointmentOrganisationId).AnyAsync(ct);
        }

        var heldPermissions = new HashSet<string>(
            actor.Permissions.Select(p => p.RoleKey),
            StringComparer.OrdinalIgnoreCase);

        foreach (var checkKey in new[]
        {
            "Admin",
            Permissions.EmployeeView,
            Permissions.EmployeeEdit,
            Permissions.EmployeePrivateRecordView,
            Permissions.EmployeeSalaryView,
            Permissions.EmployeeIdentificationView,
            Permissions.EmployeeDisciplinaryView,
            Permissions.EmployeeMedicalView,
            Permissions.DirectoryPublicProfileView,
            Permissions.IntranetAnnouncementView,
            Permissions.AttendanceView,
            Permissions.LeaveView,
            Permissions.PayrollView
        })
        {
            if (!heldPermissions.Contains(checkKey) && await access.HasPermissionAsync(checkKey))
                heldPermissions.Add(checkKey);
        }

        return await ResolveCoreAsync(
            db,
            actor.UserId,
            actor.IndividualId,
            organisationId,
            actor.IsSuperAdministrator,
            hasTenantAccess,
            actor.IsOrganisationAdmin || actor.IsHeadOfOrganisation || actor.IsHRHead || actor.IsHRManager,
            actor.IsHRStaff,
            actor.IsSupervisor,
            heldPermissions,
            permissionFilter,
            ct);
    }

    public static async Task<ResolvedOrganisationalAccess> ResolveForUserAsync(
        HrmTeContext db,
        int userId,
        int organisationId,
        string? permissionFilter = null,
        CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.UserId,
                u.BusinessEntityID,
                u.UserOrganisationId,
                u.IsActive,
                u.LocalUserStateId,
                u.AdminPortalAccessEnabled,
                u.AdminPortalAccessOrganisationId
            })
            .SingleOrDefaultAsync(ct);

        if (user is null)
        {
            return new ResolvedOrganisationalAccess
            {
                UserId = userId,
                OrganisationId = organisationId,
                HasOrganisationTenantAccess = false
            };
        }

        var isSuper = await PlatformAdministratorAccess.HasAsync(db, userId);
        var hasTenantAccess = isSuper || await db.UserOrganisations.AsNoTracking()
            .AnyAsync(o => o.UserOrganisationID == user.UserOrganisationId && o.BusinessEntityID == organisationId, ct);

        if (!hasTenantAccess && user.BusinessEntityID > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            hasTenantAccess = await (
                from a in db.LeadershipAppointments.AsNoTracking()
                join o in db.LeadershipAppointmentOrganisations.AsNoTracking()
                    on a.LeadershipAppointmentId equals o.LeadershipAppointmentId
                where a.IndividualId == user.BusinessEntityID &&
                      a.Status == "Active" &&
                      a.EffectiveFrom <= today && (!a.EffectiveTo.HasValue || a.EffectiveTo >= today) &&
                      o.OrganisationId == organisationId &&
                      o.EffectiveFrom <= today && (!o.EffectiveTo.HasValue || o.EffectiveTo >= today)
                select o.LeadershipAppointmentOrganisationId).AnyAsync(ct);
        }

        var groupNames = await db.UserAssignedUserGroups.AsNoTracking()
            .Where(x => x.UserId == userId &&
                        (x.UserGroup.IsGlobal || x.UserGroup.UserOrganisationId == user.UserOrganisationId))
            .Select(x => x.UserGroup.GroupName)
            .ToListAsync(ct);

        var directRoles = await db.UserRoles.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.UserOrganisationId == user.UserOrganisationId)
            .Select(x => x.Role.RoleKey)
            .ToListAsync(ct);

        var groupRoles = await db.UserAssignedUserGroups.AsNoTracking()
            .Where(x => x.UserId == userId &&
                        (x.UserGroup.IsGlobal || x.UserGroup.UserOrganisationId == user.UserOrganisationId))
            .SelectMany(x => x.UserGroup.UserGroupRoles.Where(r => r.IsActive).Select(r => r.Role.RoleKey))
            .ToListAsync(ct);

        var heldPermissions = new HashSet<string>(
            directRoles.Concat(groupRoles).Where(k => !string.IsNullOrWhiteSpace(k)),
            StringComparer.OrdinalIgnoreCase);

        bool HasGroup(params string[] names) =>
            groupNames.Any(g => names.Any(n => g.Equals(n, StringComparison.OrdinalIgnoreCase)));

        var isCentralHrOrAdmin = isSuper ||
                                 heldPermissions.Contains("Admin") ||
                                 HasGroup("Organisation Admin", "HR Head", "HR Manager", "Head of Organisation");
        var isHrStaff = HasGroup("HR Staff", "HR Officer", "HR");
        var isSupervisor = HasGroup("Supervisor") ||
                           await db.OrganisationStructureSupervisors.AsNoTracking()
                               .AnyAsync(s => s.SupervisorStaffId == user.BusinessEntityID && s.IsActive && s.IsValid, ct) ||
                           await db.TeamStaffs.AsNoTracking()
                               .AnyAsync(t => t.StaffId == user.BusinessEntityID && t.IsSuperVisor && t.IsValid, ct);

        return await ResolveCoreAsync(
            db,
            user.UserId,
            user.BusinessEntityID,
            organisationId,
            isSuper,
            hasTenantAccess,
            isCentralHrOrAdmin,
            isHrStaff,
            isSupervisor,
            heldPermissions,
            permissionFilter,
            ct);
    }

    private static async Task<ResolvedOrganisationalAccess> ResolveCoreAsync(
        HrmTeContext db,
        int userId,
        int individualId,
        int organisationId,
        bool isSuperAdmin,
        bool hasTenantAccess,
        bool isCentralHrOrAdmin,
        bool isHrStaff,
        bool isSupervisor,
        HashSet<string> heldPermissions,
        string? permissionFilter,
        CancellationToken ct)
    {
        await EnsureSchemaAsync(db, ct);
        var now = DateTime.UtcNow;
        var today = TodayMaldives;

        var groupIds = userId > 0
            ? await db.UserAssignedUserGroups.AsNoTracking()
                .Where(x => x.UserId == userId &&
                            (x.UserGroup.IsGlobal || x.UserGroup.UserOrganisation!.BusinessEntityID == organisationId))
                .Select(x => x.UserGroupId)
                .ToArrayAsync(ct)
            : [];

        List<UserAccessScope> activeGrants = [];
        if (userId > 0 && organisationId > 0)
        {
            try
            {
                activeGrants = await db.UserAccessScopes.AsNoTracking()
                    .Where(s => s.OrganisationId == organisationId &&
                                s.IsActive &&
                                s.ValidFrom <= now &&
                                (!s.ValidTo.HasValue || s.ValidTo.Value > now) &&
                                (s.UserId == userId || (s.UserGroupId.HasValue && groupIds.Contains(s.UserGroupId.Value))))
                    .ToListAsync(ct);
            }
            catch
            {
                activeGrants = [];
            }
        }

        if (!string.IsNullOrWhiteSpace(permissionFilter))
        {
            activeGrants = activeGrants
                .Where(g => string.IsNullOrWhiteSpace(g.PermissionFilter) ||
                            g.PermissionFilter == "*" ||
                            g.PermissionFilter.Equals(permissionFilter, StringComparison.OrdinalIgnoreCase) ||
                            permissionFilter.StartsWith(g.PermissionFilter + ".", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (activeGrants.Count > 0)
            hasTenantAccess = true;

        if (!hasTenantAccess)
        {
            return new ResolvedOrganisationalAccess
            {
                UserId = userId,
                IndividualId = individualId,
                OrganisationId = organisationId,
                IsSuperAdministrator = isSuperAdmin,
                HasOrganisationTenantAccess = false,
                HasFullOrganisationScope = false
            };
        }

        var hasExplicitGrants = activeGrants.Count > 0;
        var hasExplicitOrgGrant = activeGrants.Any(g =>
            g.ScopeType.Equals(AccessScopeTypes.Organisation, StringComparison.OrdinalIgnoreCase));

        // Load organisation structure hierarchy for descendant expansion
        var allStructureIds = await db.OrganisationStructures.AsNoTracking()
            .Where(s => s.OrganisationBusinessEntityID == organisationId && s.OrganisationStructureStateId == 3)
            .Select(s => s.OrganisationStructureId)
            .ToListAsync(ct);

        var relations = await db.OrganisationStructureRelations.AsNoTracking()
            .Where(r => r.IsCurrent && allStructureIds.Contains(r.OrganisationStructureId))
            .Select(r => new { r.OrganisationStructureId, r.ParentOrganisationStructureId })
            .ToListAsync(ct);

        var childStructureLookup = relations.ToLookup(r => r.ParentOrganisationStructureId, r => r.OrganisationStructureId);

        void AddStructureWithOptionalDescendants(HashSet<int> target, int rootUnitId, bool includeDescendants)
        {
            if (!allStructureIds.Contains(rootUnitId)) return;
            var queue = new Queue<int>();
            if (target.Add(rootUnitId) && includeDescendants)
                queue.Enqueue(rootUnitId);
            else if (includeDescendants)
                queue.Enqueue(rootUnitId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var childId in childStructureLookup[current])
                {
                    if (target.Add(childId))
                        queue.Enqueue(childId);
                }
            }
        }

        var allTeams = await db.Teams.AsNoTracking()
            .Where(t => t.OrganisationId == organisationId && t.IsValid == true &&
                        (!t.StartDate.HasValue || t.StartDate <= today) &&
                        (!t.EndDate.HasValue || t.EndDate >= today))
            .Select(t => new { t.TeamId, t.ParentTeamId, t.OrganisationStructureId })
            .ToListAsync(ct);

        var childTeamLookup = allTeams
            .Where(t => t.ParentTeamId.HasValue)
            .ToLookup(t => t.ParentTeamId!.Value, t => t.TeamId);

        void AddTeamWithOptionalDescendants(HashSet<int> target, int rootTeamId, bool includeDescendants)
        {
            var queue = new Queue<int>();
            target.Add(rootTeamId);
            if (includeDescendants)
                queue.Enqueue(rootTeamId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var childTeamId in childTeamLookup[current])
                {
                    if (target.Add(childTeamId))
                        queue.Enqueue(childTeamId);
                }
            }
        }

        var authorisedStructures = new HashSet<int>();
        var authorisedTeams = new HashSet<int>();

        foreach (var grant in activeGrants)
        {
            if (grant.ScopeType.Equals(AccessScopeTypes.Organisation, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var sid in allStructureIds)
                    authorisedStructures.Add(sid);
                foreach (var t in allTeams)
                    authorisedTeams.Add(t.TeamId);
            }
            else if (grant.ScopeType.Equals(AccessScopeTypes.Division, StringComparison.OrdinalIgnoreCase) ||
                     grant.ScopeType.Equals(AccessScopeTypes.DivisionAndDescendants, StringComparison.OrdinalIgnoreCase) ||
                     grant.ScopeType.Equals(AccessScopeTypes.Department, StringComparison.OrdinalIgnoreCase) ||
                     grant.ScopeType.Equals(AccessScopeTypes.Custom, StringComparison.OrdinalIgnoreCase))
            {
                if (grant.UnitId.HasValue)
                {
                    var includeChildren = grant.IncludeDescendants ||
                                          grant.ScopeType.Equals(AccessScopeTypes.DivisionAndDescendants, StringComparison.OrdinalIgnoreCase);
                    AddStructureWithOptionalDescendants(authorisedStructures, grant.UnitId.Value, includeChildren);
                }
            }
            else if (grant.ScopeType.Equals(AccessScopeTypes.Team, StringComparison.OrdinalIgnoreCase))
            {
                if (grant.TeamId.HasValue)
                {
                    AddTeamWithOptionalDescendants(authorisedTeams, grant.TeamId.Value, grant.IncludeDescendants);
                }
                else if (individualId > 0)
                {
                    var myTeamIds = await db.TeamStaffs.AsNoTracking()
                        .Where(m => m.StaffId == individualId && m.IsValid &&
                                    m.StartDate <= today && (!m.EndDate.HasValue || m.EndDate >= today))
                        .Select(m => m.TeamId)
                        .ToListAsync(ct);
                    foreach (var tid in myTeamIds)
                        AddTeamWithOptionalDescendants(authorisedTeams, tid, grant.IncludeDescendants);
                }
            }
        }

        // Also include units and teams where the actor is an active designated supervisor / head-in-charge
        // unless their only explicit scope is strictly Self.
        var isStrictlySelfOnly = hasExplicitGrants &&
                                 activeGrants.All(g => g.ScopeType.Equals(AccessScopeTypes.Self, StringComparison.OrdinalIgnoreCase));

        if (!isStrictlySelfOnly && individualId > 0)
        {
            var supervisedStructureIds = await db.OrganisationStructureSupervisors.AsNoTracking()
                .Where(s => s.SupervisorStaffId == individualId && s.IsActive && s.IsValid &&
                            allStructureIds.Contains(s.OrganisationStructureId))
                .Select(s => s.OrganisationStructureId)
                .ToListAsync(ct);

            var headStructureIds = await db.OrganisationStructureHeadIncharges.AsNoTracking()
                .Where(h => h.IndividualId == individualId && h.IsValid &&
                            allStructureIds.Contains(h.OrganisationStructureId))
                .Select(h => h.OrganisationStructureId)
                .ToListAsync(ct);

            foreach (var sid in supervisedStructureIds.Concat(headStructureIds))
                AddStructureWithOptionalDescendants(authorisedStructures, sid, includeDescendants: true);

            var supervisedTeams = await db.TeamStaffs.AsNoTracking()
                .Where(m => m.StaffId == individualId && m.IsSuperVisor && m.IsValid &&
                            m.StartDate <= today && (!m.EndDate.HasValue || m.EndDate >= today))
                .Select(m => m.TeamId)
                .ToListAsync(ct);

            foreach (var tid in supervisedTeams)
                AddTeamWithOptionalDescendants(authorisedTeams, tid, includeDescendants: true);
        }

        // Any team belonging to an authorised structure is also within scope
        foreach (var team in allTeams)
        {
            if (team.OrganisationStructureId.HasValue && authorisedStructures.Contains(team.OrganisationStructureId.Value))
                authorisedTeams.Add(team.TeamId);
        }

        // Determine if user has full organisation-wide scope:
        // 1. SuperAdmin always has full scope.
        // 2. Explicit Organisation scope grant gives full scope.
        // 3. If no explicit UserAccessScopes are configured for this user/role, Central HR / Admin / un-scoped HR with module permissions
        //    have organisation-wide scope (preserving backward compatibility while enforcing isolation whenever scopes are assigned).
        var hasBroadHrOrModuleRole = isCentralHrOrAdmin || isHrStaff ||
                                     heldPermissions.Contains("Admin") ||
                                     heldPermissions.Contains(Permissions.EmployeeView) ||
                                     heldPermissions.Contains(Permissions.EmployeeEdit) ||
                                     heldPermissions.Contains(Permissions.AttendanceView) ||
                                     heldPermissions.Contains(Permissions.LeaveView) ||
                                     heldPermissions.Contains(Permissions.PayrollView);

        var hasFullOrganisationScope = isSuperAdmin ||
                                       hasExplicitOrgGrant ||
                                       (!hasExplicitGrants && hasBroadHrOrModuleRole);

        if (hasFullOrganisationScope)
        {
            foreach (var sid in allStructureIds)
                authorisedStructures.Add(sid);
            foreach (var t in allTeams)
                authorisedTeams.Add(t.TeamId);
        }

        // Calculate Field-Level / Data Classification permissions
        var hasUnscopedLegacyHrPrivilege = !hasExplicitGrants && (isCentralHrOrAdmin || isHrStaff);
        var hasFullAdminOverride = isSuperAdmin || isCentralHrOrAdmin || heldPermissions.Contains("Admin");

        var fieldPermissions = new DataClassificationAccess
        {
            CanViewPublicProfile = true,
            CanViewBasicEmployment = hasFullOrganisationScope || authorisedStructures.Count > 0 || authorisedTeams.Count > 0 ||
                                     heldPermissions.Contains(Permissions.EmployeeView) ||
                                     heldPermissions.Contains(Permissions.DirectoryPublicProfileView) ||
                                     isSupervisor,
            CanViewPrivateRecord = hasFullAdminOverride ||
                                   heldPermissions.Contains(Permissions.EmployeePrivateRecordView) ||
                                   hasUnscopedLegacyHrPrivilege,
            CanViewSalaryAndBank = hasFullAdminOverride ||
                                   heldPermissions.Contains(Permissions.EmployeeSalaryView) ||
                                   heldPermissions.Contains(Permissions.PayrollView) ||
                                   hasUnscopedLegacyHrPrivilege,
            CanViewIdentification = hasFullAdminOverride ||
                                    heldPermissions.Contains(Permissions.EmployeeIdentificationView) ||
                                    heldPermissions.Contains(Permissions.EmployeePrivateRecordView) ||
                                    hasUnscopedLegacyHrPrivilege,
            CanViewDisciplinary = hasFullAdminOverride ||
                                  heldPermissions.Contains(Permissions.EmployeeDisciplinaryView),
            CanViewMedical = hasFullAdminOverride ||
                             heldPermissions.Contains(Permissions.EmployeeMedicalView),
            CanViewIntranetAnnouncements = true
        };

        return new ResolvedOrganisationalAccess
        {
            UserId = userId,
            IndividualId = individualId,
            OrganisationId = organisationId,
            IsSuperAdministrator = isSuperAdmin,
            HasOrganisationTenantAccess = true,
            HasFullOrganisationScope = hasFullOrganisationScope,
            HasExplicitScopeGrants = hasExplicitGrants,
            AuthorisedStructureIds = authorisedStructures,
            AuthorisedTeamIds = authorisedTeams,
            ActiveGrants = activeGrants,
            FieldPermissions = fieldPermissions
        };
    }

    /// <summary>
    /// Determines whether the current user can view a target employee in the organisation,
    /// using effective job assignments (and optional historical period assignments).
    /// </summary>
    public static async Task<bool> CanViewEmployeeAsync(
        HrmTeContext db,
        IUserAccessService access,
        int targetIndividualId,
        int organisationId,
        DateTime? periodFrom = null,
        DateTime? periodTo = null,
        CancellationToken ct = default)
    {
        var resolved = await ResolveCurrentUserAsync(db, access, organisationId, ct: ct);
        return await CanViewEmployeeWithResolvedScopeAsync(db, resolved, targetIndividualId, periodFrom, periodTo, ct);
    }

    public static async Task<bool> CanViewEmployeeWithResolvedScopeAsync(
        HrmTeContext db,
        ResolvedOrganisationalAccess resolved,
        int targetIndividualId,
        DateTime? periodFrom = null,
        DateTime? periodTo = null,
        CancellationToken ct = default)
    {
        if (targetIndividualId <= 0) return false;
        if (resolved.IsSuperAdministrator || (resolved.IndividualId > 0 && resolved.IndividualId == targetIndividualId))
            return true;
        if (!resolved.HasOrganisationTenantAccess)
            return false;
        if (resolved.HasFullOrganisationScope)
            return true;

        var today = TodayMaldives;
        var fromDate = periodFrom?.Date ?? today;
        var toDate = periodTo?.Date ?? today;

        if (resolved.AuthorisedStructureIds.Count > 0)
        {
            var structureIds = resolved.AuthorisedStructureIds.ToArray();

            // Check active/effective Jobs in authorised structures
            var hasJobInScope = await db.Jobs.AsNoTracking()
                .AnyAsync(j => j.IndividualID == targetIndividualId &&
                               j.OrganisationID == resolved.OrganisationId &&
                               j.JobStateId == SharedConfig.JobStates.APPROVED &&
                               j.JoinedDate <= toDate &&
                               (!j.TerminatedDate.HasValue || j.TerminatedDate > fromDate) &&
                               structureIds.Contains(j.OrganisationStructureId), ct);
            if (hasJobInScope) return true;

            // Check historical effective-dated ST_JobOrganisationHistories for period-accurate access
            var hasHistoryInScope = await db.JobOrganisationHistories.AsNoTracking()
                .AnyAsync(h => h.IndividualId == targetIndividualId &&
                               h.OrganisationId == resolved.OrganisationId &&
                               h.EffectiveFrom <= toDate &&
                               (!h.EffectiveTo.HasValue || h.EffectiveTo >= fromDate) &&
                               structureIds.Contains(h.OrganisationStructureId), ct);
            if (hasHistoryInScope) return true;

            // Check active internal/external transfer workflow involving origin or destination structure
            var hasTransferInScope = await db.StaffTransfers.AsNoTracking()
                .AnyAsync(t => t.IndividualId == targetIndividualId &&
                               t.IsOpen &&
                               ((t.FromOrganisationId == resolved.OrganisationId && structureIds.Contains(t.FromOrganisationStructureId)) ||
                                (t.ToOrganisationId == resolved.OrganisationId && structureIds.Contains(t.ToOrganisationStructureId))), ct);
            if (hasTransferInScope) return true;
        }

        if (resolved.AuthorisedTeamIds.Count > 0)
        {
            var teamIds = resolved.AuthorisedTeamIds.ToArray();
            var hasTeamInScope = await db.TeamStaffs.AsNoTracking()
                .AnyAsync(m => m.StaffId == targetIndividualId &&
                               m.IsValid &&
                               m.StartDate <= toDate &&
                               (!m.EndDate.HasValue || m.EndDate >= fromDate) &&
                               teamIds.Contains(m.TeamId), ct);
            if (hasTeamInScope) return true;
        }

        return false;
    }

    /// <summary>
    /// Filters a set of candidate employee IndividualIds to those within the current user's authorised organisational scope
    /// using batched SQL queries instead of per-row database roundtrips.
    /// </summary>
    public static async Task<HashSet<int>> FilterAuthorisedEmployeesAsync(
        HrmTeContext db,
        ResolvedOrganisationalAccess resolved,
        IEnumerable<int> candidateIndividualIds,
        DateTime? periodFrom = null,
        DateTime? periodTo = null,
        CancellationToken ct = default)
    {
        var candidates = candidateIndividualIds.Where(id => id > 0).Distinct().ToArray();
        if (candidates.Length == 0) return [];
        if (resolved.IsSuperAdministrator) return [.. candidates];
        if (!resolved.HasOrganisationTenantAccess)
            return resolved.IndividualId > 0 && candidates.Contains(resolved.IndividualId)
                ? [resolved.IndividualId]
                : [];
        if (resolved.HasFullOrganisationScope)
            return [.. candidates];

        var result = new HashSet<int>();
        if (resolved.IndividualId > 0 && candidates.Contains(resolved.IndividualId))
            result.Add(resolved.IndividualId);

        var today = TodayMaldives;
        var fromDate = periodFrom?.Date ?? today;
        var toDate = periodTo?.Date ?? today;

        if (resolved.AuthorisedStructureIds.Count > 0)
        {
            var structureIds = resolved.AuthorisedStructureIds.ToArray();

            var jobMatches = await db.Jobs.AsNoTracking()
                .Where(j => candidates.Contains(j.IndividualID) &&
                            j.OrganisationID == resolved.OrganisationId &&
                            j.JobStateId == SharedConfig.JobStates.APPROVED &&
                            j.JoinedDate <= toDate &&
                            (!j.TerminatedDate.HasValue || j.TerminatedDate > fromDate) &&
                            structureIds.Contains(j.OrganisationStructureId))
                .Select(j => j.IndividualID)
                .Distinct()
                .ToListAsync(ct);

            foreach (var id in jobMatches)
                result.Add(id);

            var historyMatches = await db.JobOrganisationHistories.AsNoTracking()
                .Where(h => candidates.Contains(h.IndividualId) &&
                            h.OrganisationId == resolved.OrganisationId &&
                            h.EffectiveFrom <= toDate &&
                            (!h.EffectiveTo.HasValue || h.EffectiveTo >= fromDate) &&
                            structureIds.Contains(h.OrganisationStructureId))
                .Select(h => h.IndividualId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var id in historyMatches)
                result.Add(id);
        }

        if (resolved.AuthorisedTeamIds.Count > 0)
        {
            var teamIds = resolved.AuthorisedTeamIds.ToArray();
            var teamMatches = await db.TeamStaffs.AsNoTracking()
                .Where(m => candidates.Contains(m.StaffId) &&
                            m.IsValid &&
                            m.StartDate <= toDate &&
                            (!m.EndDate.HasValue || m.EndDate >= fromDate) &&
                            teamIds.Contains(m.TeamId))
                .Select(m => m.StaffId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var id in teamMatches)
                result.Add(id);
        }

        return result;
    }
}
