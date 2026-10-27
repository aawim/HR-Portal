using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class OrganisationPermissionService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService logs, IUserContextService contexts)
:global::HRM.Services.Contracts.OrganisationPermissionServiceContracts,global::HRM.Services.Contracts.OrganisationPermissionServiceContracts.IOperations{
    // Platform permission management must remain recoverable after a tenant ceiling is reduced.
    // This checks an explicit assignment; organisation-default permissions cannot confer platform authority.
    private static async Task<bool> SuperAdmin(HrmTeContext db, int id) => await db.Users.AnyAsync(u => u.UserId == id &&
        u.IsActive && u.LocalUserStateId == 1 && u.AdminPortalAccessEnabled && u.AdminPortalAccessOrganisationId == u.UserOrganisationId &&
        (u.UserRoles.Any(r => r.IsActive && r.UserOrganisationId == u.UserOrganisationId && r.Role.RoleKey == "Super.Admin.Manage") ||
         u.UserAssignedUserGroups.Any(g => (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisationId == u.UserOrganisationId) &&
             g.UserGroup.UserGroupRoles.Any(r => r.IsActive && r.Role.RoleKey == "Super.Admin.Manage"))));

    private async Task<(UserContextDto Actor, int Org, bool Super, bool Edit)> Scope(int? selected)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var super = await SuperAdmin(db, actor.UserId);
        var current = await access.GetCurrentOrganisationIdAsync();
        var org = super && selected.GetValueOrDefault() > 0 ? selected!.Value : current;
        if (super && org <= 0) org = await db.UserOrganisations.OrderBy(x => x.Organisation.OrganisationName).Select(x => x.BusinessEntityID).FirstOrDefaultAsync();
        if (org <= 0 || !super && (selected.HasValue && selected.Value != current || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Use your current organisation.");
        var edit = super || await access.HasPermissionAsync("UserOrganisations.Roles.Edit") || await access.HasPermissionAsync("Admin");
        if (!edit && !await access.HasPermissionAsync("UserOrganisations.View") && !await access.HasPermissionAsync("UserOrganisations.Admin.View"))
            throw new UnauthorizedAccessException("You do not have permission to view organisation permissions.");
        return (actor, org, super, edit);
    }
    public async Task<OrganisationPermissionData> LoadAsync(int? organisationId = null)
    {
        var (actor, org, super, edit) = await Scope(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var entity = await db.UserOrganisations.AsNoTracking().Where(x => x.BusinessEntityID == org)
            .Select(x => new { x.UserOrganisationID, x.Organisation.OrganisationName }).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("This organisation has no user organisation configuration.");
        var result = new OrganisationPermissionData { OrganisationId = org, OrganisationName = entity.OrganisationName,
            IsSuperAdmin = super, CanEdit = edit, AccountCount = await db.Users.CountAsync(u => u.UserOrganisationId == entity.UserOrganisationID) };
        result.Organisations = await db.UserOrganisations.Where(o => super || o.BusinessEntityID == org)
            .OrderBy(o => o.Organisation.OrganisationName).Select(o => new PermissionSettingOption(o.BusinessEntityID, o.Organisation.OrganisationName)).ToListAsync();
        result.Permissions = await db.Roles.AsNoTracking().OrderBy(p => p.RoleKey).Select(p => new PermissionDefinitionDto {
            Id = p.RoleID, Key = p.RoleKey, Name = p.Name, Description = p.Description ?? "", ContextId = p.ContextId }).ToListAsync();
        result.AllowedIds = await db.UserOrganisationRoles.Where(p => p.UserOrganisationID == entity.UserOrganisationID && p.IsActive).Select(p => p.RoleID).ToListAsync();
        result.AssignableIds = super ? result.Permissions.Select(p => p.Id).ToList() : actor.Permissions.Select(p => p.RoleID).ToList();
        return result;
    }
    public async Task<ServiceResult> SaveAsync(int organisationId, IEnumerable<int> selectedIds, IEnumerable<int> expectedIds, string reason)
    {
        var (actor, org, super, edit) = await Scope(organisationId);
        if (!edit) throw new UnauthorizedAccessException("You cannot change organisation permissions.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) return ServiceResult.Fail("Enter a reason of up to 1,000 characters.");
        var desired = selectedIds.Distinct().ToHashSet(); var expected = expectedIds.ToHashSet();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity = await db.UserOrganisations.Include(x => x.UserOrganisationRoles).SingleOrDefaultAsync(x => x.BusinessEntityID == org)
            ?? throw new InvalidOperationException("The organisation configuration no longer exists.");
        var existing = entity.UserOrganisationRoles.Where(x => x.IsActive).Select(x => x.RoleID).ToHashSet();
        if (!existing.SetEquals(expected)) return ServiceResult.Fail("Organisation permissions changed since you opened the page. Refresh before saving.");
        if (await db.Roles.CountAsync(p => desired.Contains(p.RoleID)) != desired.Count) return ServiceResult.Fail("A selected permission no longer exists. Refresh the page.");
        if (!super && desired.Except(existing).Any(id => !actor.Permissions.Any(p => p.RoleID == id)))
            return ServiceResult.Fail("Only Super Admin can enable permissions outside your organisation's current access.");
        if (!super && !await db.Roles.AnyAsync(p => desired.Contains(p.RoleID) && (p.RoleKey == "Admin" || p.RoleKey == "UserOrganisations.Roles.Edit")))
            return ServiceResult.Fail("Keep organisation permission management available, or ask Super Admin to make this change.");
        if (existing.SetEquals(desired)) return ServiceResult.Ok("No permission changes to save.");
        var added = string.Join(",", desired.Except(existing).Order()); var removed = string.Join(",", existing.Except(desired).Order());
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ORGANISATION_ROLE_UPDATE,
            $"Organisation {org} permission limits: added [{added}]; removed [{removed}]. {reason.Trim()}");
        foreach (var p in entity.UserOrganisationRoles) { p.IsActive = desired.Contains(p.RoleID); p.OperationLogId = audit.OperationLogId; }
        foreach (var id in desired.Except(entity.UserOrganisationRoles.Select(p => p.RoleID))) entity.UserOrganisationRoles.Add(new() { RoleID = id, IsActive = true, OperationLogId = audit.OperationLogId });
        entity.OperationLogId = audit.OperationLogId;
        var users = await db.Users.Where(u => u.UserOrganisationId == entity.UserOrganisationID).Select(u => u.UserId).ToListAsync();
        await db.SaveChangesAsync(); await tx.CommitAsync();
        foreach (var id in users) await contexts.InvalidateAsync(id);
        return ServiceResult.Ok("Organisation permissions saved. Direct and User Group access is limited to these permissions.");
    }
}
