using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class PermissionSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService logs, IUserContextService contexts)
:global::HRM.Services.Contracts.PermissionSettingsServiceContracts,global::HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations{
    private async Task<UserContextDto> RequireAsync(string permission, bool groups)
    {
        var user = await access.RequireContextAsync();
        if (!user.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") &&
            !(groups && await access.HasPermissionAsync("Developer")) &&
            !await access.HasPermissionAsync(permission) &&
            !(groups && await access.HasPermissionAsync(permission.Replace("UserOrganisation", "Organisation").Replace(".Create", ".Add"))))
            throw new UnauthorizedAccessException("You do not have permission to manage these access settings.");
        return user;
    }

    private async Task<int> OrganisationAsync(UserContextDto user, int? requestedOrgId = null)
    {
        var org = user.IsSuperAdministrator && requestedOrgId is > 0
            ? requestedOrgId.Value
            : user.Organisation?.OrganisationBusinessEntityId ?? 0;
        if (org == 0 && user.ActiveJob != null)
        {
            await using var db = await factory.CreateDbContextAsync();
            org = await db.Jobs.Where(x => x.JobId == user.ActiveJob.JobId).Select(x => x.OrganisationID).SingleAsync();
        }
        if (org <= 0 || (!user.IsSuperAdministrator && !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Select an organisation you can manage.");
        return org;
    }

    public Task<PermissionSettingsData> LoadAsync(bool groups) => LoadForOrganisationAsync(groups, null);

    public async Task<PermissionSettingsData> LoadForOrganisationAsync(bool groups, int? organisationId)
    {
        var user = await RequireAsync(groups ? "UserOrganisation.Groups.View" : "Roles.View", groups);
        await using var db = await factory.CreateDbContextAsync();
        await EnsureCanonicalSiteMapCatalogAsync(db);
        var contextsList = await db.Contexts.AsNoTracking().OrderBy(x => x.Name).Select(x => new PermissionSettingOption(x.ContextId, x.Name)).ToListAsync();
        var modulesList = await db.Modules.AsNoTracking().OrderBy(x => x.Name).Select(x => new PermissionSettingOption(x.ModuleId, x.Name)).ToListAsync();
        var contextMap = contextsList.ToDictionary(c => c.Id, c => c.Name);
        var moduleMap = modulesList.ToDictionary(m => m.Id, m => m.Name);

        var rawPermissions = await db.Roles.AsNoTracking().OrderBy(x => x.RoleKey).Select(x => new PermissionDefinitionDto
        {
            Id = x.RoleID,
            Key = x.RoleKey,
            Name = x.Name,
            Description = x.Description ?? "",
            ContextId = x.ContextId,
            ModuleId = x.ModuleId,
            IsSystem = x.IsSystemRole == true
        }).ToListAsync();

        foreach (var p in rawPermissions)
        {
            p.ContextName = contextMap.TryGetValue(p.ContextId, out var cName) ? cName : "";
            p.ModuleName = p.ModuleId.HasValue && moduleMap.TryGetValue(p.ModuleId.Value, out var mName) ? mName : "Unassigned";
        }

        var result = new PermissionSettingsData
        {
            IsSuperAdmin = user.IsSuperAdministrator,
            Permissions = rawPermissions,
            Contexts = contextsList,
            Modules = modulesList,
            AreaMap = BuildSiteMapAreaCatalog(rawPermissions, contextsList)
        };
        if (groups)
        {
            var org = await OrganisationAsync(user, organisationId);
            var inactiveEntityStateIds = await HRM.Helpers.ActiveOrganisationFilter.InactiveEntityStateIdsAsync(db);
            result.Organisations = await db.UserOrganisations.AsNoTracking()
                .Where(uo => (user.IsSuperAdministrator || uo.BusinessEntityID == org) &&
                             !db.BusinessEntities.Any(b => b.BusinessEntityID == uo.BusinessEntityID && b.BusinessEntityStateId.HasValue && inactiveEntityStateIds.Contains(b.BusinessEntityStateId.Value)))
                .OrderBy(uo => uo.Organisation.OrganisationName)
                .Select(uo => new PermissionSettingOption(uo.BusinessEntityID, uo.Organisation.OrganisationName))
                .ToListAsync();
            result.OrganisationId = org;
            result.OrganisationName = result.Organisations.FirstOrDefault(o => o.Id == org)?.Name
                ?? await db.Organisations.AsNoTracking().Where(o => o.BusinessEntityID == org).Select(o => o.OrganisationName).FirstOrDefaultAsync()
                ?? "Organisation";
            result.Roles = await db.UserGroups.AsNoTracking().AsSplitQuery().Where(x => x.UserOrganisation.BusinessEntityID == org)
                .OrderBy(x => x.GroupName).Select(x => new AccessRoleDto
                { Id = x.UserGroupId, OrganisationId = org, Name = x.GroupName,
                    MemberCount = x.UserAssignedUserGroups.Count(a => db.Users.Any(u => u.UserId == a.UserId)),
                    PermissionIds = x.UserGroupRoles.Where(r => r.IsActive).Select(r => r.RoleId).ToList(),
                    Members = x.UserAssignedUserGroups.OrderBy(a => a.User.FullName).Select(a => new RoleStaffDto
                    {
                        UserId = a.UserId, Username = a.User.Username, IsActive = a.User.IsActive,
                        Name = a.User.BusinessEntity != null
                            ? (a.User.BusinessEntity.FirstNameEnglish + " " + (a.User.BusinessEntity.MiddleNameEnglish ?? "") + " " + (a.User.BusinessEntity.LastNameEnglish ?? "")).Replace("  ", " ").Trim()
                            : a.User.FullName ?? a.User.Username
                    }).ToList() }).ToListAsync();

            var todayDate = DateTime.Today;
            var userOrgId = await db.UserOrganisations.AsNoTracking()
                .Where(uo => uo.BusinessEntityID == org)
                .Select(uo => (int?)uo.UserOrganisationID)
                .FirstOrDefaultAsync();
            result.OrganisationUsers = await db.Users.AsNoTracking()
                .Where(u =>
                    db.Jobs.Any(j => j.IndividualID == u.BusinessEntityID && j.OrganisationID == org && j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= todayDate) ||
                    (userOrgId.HasValue && u.UserOrganisationId == userOrgId.Value &&
                     !db.Jobs.Any(j => j.IndividualID == u.BusinessEntityID && j.OrganisationID != org && j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= todayDate)))
                .Select(u => new RoleStaffDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    IsActive = u.IsActive,
                    Name = u.BusinessEntity != null
                        ? (u.BusinessEntity.FirstNameEnglish + " " + (u.BusinessEntity.MiddleNameEnglish ?? "") + " " + (u.BusinessEntity.LastNameEnglish ?? "")).Replace("  ", " ").Trim()
                        : u.FullName ?? u.Username
                })
                .OrderBy(u => u.Name)
                .ToListAsync();
        }
        return result;
    }

    public async Task<ServiceResult> SavePermissionAsync(PermissionDefinitionDto model)
    {
        var actor = await RequireAsync(model.Id == 0 ? "Roles.Add" : "Roles.Edit", false);
        if (!actor.IsSuperAdministrator)
            throw new UnauthorizedAccessException("Only Super Admin can create or change permission definitions.");
        if (string.IsNullOrWhiteSpace(model.Key) || model.Key.Length > 100 || model.Key.Any(char.IsWhiteSpace) ||
            string.IsNullOrWhiteSpace(model.Name) || model.Name.Length > 250 || model.Description.Length > 1000)
            return ServiceResult.Fail("Enter a key without spaces (up to 100 characters), a name (up to 250) and description (up to 1000).");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.Roles.AnyAsync(x => x.RoleID != model.Id && x.RoleKey == model.Key.Trim()))
            return ServiceResult.Fail("This permission key already exists.");
        if (!await db.Contexts.AnyAsync(x => x.ContextId == model.ContextId) ||
            model.ModuleId.HasValue && !await db.Modules.AnyAsync(x => x.ModuleId == model.ModuleId))
            return ServiceResult.Fail("Select a valid context and module.");
        var entity = model.Id == 0 ? new Role() : await db.Roles.SingleOrDefaultAsync(x => x.RoleID == model.Id);
        if (entity == null) return ServiceResult.Fail("Permission was not found.");
        if (model.Id != 0 && entity.RoleKey != model.Key) return ServiceResult.Fail("Existing permission keys cannot be renamed because application checks depend on them.");
        var log = await logs.CreateAsync(db, model.Id == 0 ? SharedConfig.OperationLogActionTypes.ROLES_CREATE : SharedConfig.OperationLogActionTypes.ROLES_UPDATE,
            $"Saved permission {model.Key}");
        if (model.Id == 0) { entity.RoleKey = model.Key.Trim(); entity.IsSystemRole = false; db.Roles.Add(entity); }
        entity.Name = model.Name.Trim(); entity.Description = model.Description.Trim();
        entity.ContextId = model.ContextId; entity.ModuleId = model.ModuleId; entity.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.RoleID;
        return ServiceResult.Ok("Permission saved.");
    }

    public async Task<ServiceResult> SaveRoleAsync(AccessRoleDto model)
    {
        var user = await RequireAsync(model.Id == 0 ? "UserOrganisation.Groups.Create" : "UserOrganisation.Groups.Edit", true);
        var org = await OrganisationAsync(user, model.OrganisationId);
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Length > 250) return ServiceResult.Fail("Enter a role name of up to 250 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var orgId = await db.UserOrganisations.Where(x => x.BusinessEntityID == org).Select(x => x.UserOrganisationID).SingleAsync();
        var entity = model.Id == 0 ? new UserGroup { UserOrganisationId = orgId } :
            await db.UserGroups.Include(x => x.UserGroupRoles).SingleOrDefaultAsync(x => x.UserGroupId == model.Id && x.UserOrganisationId == orgId);
        if (entity == null) return ServiceResult.Fail("Role was not found in this organisation.");
        if (await db.UserGroups.AnyAsync(x => x.UserOrganisationId == orgId && x.UserGroupId != model.Id && x.GroupName == model.Name.Trim()))
            return ServiceResult.Fail("A role with this name already exists.");
        var ids = model.PermissionIds.Distinct().ToList();
        if (await db.Roles.CountAsync(x => ids.Contains(x.RoleID)) != ids.Count) return ServiceResult.Fail("A selected permission no longer exists.");
        var added = ids.Except(entity.UserGroupRoles.Where(x => x.IsActive).Select(x => x.RoleId)).ToList();
        if (await db.Roles.AnyAsync(x => ids.Contains(x.RoleID) && x.RoleKey == "Super.Admin.Manage"))
            return ServiceResult.Fail("Platform administration is a direct, audited grant and cannot be assigned to a role group.");
        var allowedIds = await db.UserOrganisationRoles.Where(x => x.UserOrganisationID == orgId && x.IsActive)
            .Select(x => x.RoleID).ToListAsync();
        if (!user.IsSuperAdministrator && ids.Except(allowedIds).Any())
            return ServiceResult.Fail("A role can only contain permissions enabled for this organisation.");
        if (!user.IsSuperAdministrator && added.Any(id => !user.Permissions.Any(p => p.RoleID == id)))
            return ServiceResult.Fail("You can only grant permissions that you hold.");
        var log = await logs.CreateAsync(db, model.Id == 0 ? SharedConfig.OperationLogActionTypes.USERS_GROUP_CREATE : SharedConfig.OperationLogActionTypes.USERS_GROUP_UPDATE, $"Saved role {model.Name}");
        if (model.Id == 0) db.UserGroups.Add(entity);
        entity.GroupName = model.Name.Trim(); entity.OperationLogId = log.OperationLogId;
        foreach (var link in entity.UserGroupRoles) { link.IsActive = ids.Contains(link.RoleId); link.OperationLogId = log.OperationLogId; }
        foreach (var id in ids.Except(entity.UserGroupRoles.Select(x => x.RoleId)))
            entity.UserGroupRoles.Add(new() { RoleId = id, IsActive = true, OperationLogId = log.OperationLogId });
        var memberIds = await db.UserAssignedUserGroups.Where(x => x.UserGroupId == entity.UserGroupId).Select(x => x.UserId).ToListAsync();
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.UserGroupId;
        foreach (var id in memberIds) await contexts.InvalidateAsync(id);
        return ServiceResult.Ok("Role and grouped permissions saved.");
    }

    public async Task<ServiceResult> AddStaffToRoleAsync(int roleId, int userId, int? organisationId)
    {
        var actor = await RequireAsync("Users.Roles.Edit", true);
        var org = await OrganisationAsync(actor, organisationId);
        if (!actor.IsSuperAdministrator && actor.UserId == userId)
            return ServiceResult.Fail("You cannot add roles to your own account.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var orgId = await db.UserOrganisations.Where(x => x.BusinessEntityID == org).Select(x => x.UserOrganisationID).SingleAsync();
        var roleGroup = await db.UserGroups.Include(g => g.UserGroupRoles)
            .SingleOrDefaultAsync(g => g.UserGroupId == roleId && g.UserOrganisationId == orgId);
        if (roleGroup == null) return ServiceResult.Fail("Role was not found in this organisation.");

        var today = DateTime.Today;
        var target = await db.Users.SingleOrDefaultAsync(u => u.UserId == userId && (
            db.Jobs.Any(j => j.IndividualID == u.BusinessEntityID && j.OrganisationID == org && j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= today) ||
            (u.UserOrganisationId == orgId &&
             !db.Jobs.Any(j => j.IndividualID == u.BusinessEntityID && j.OrganisationID != org && j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= today))));
        if (target == null) return ServiceResult.Fail("Staff account was not found in this organisation.");

        if (!actor.IsSuperAdministrator)
        {
            var groupPermIds = roleGroup.UserGroupRoles.Where(r => r.IsActive).Select(r => r.RoleId).ToList();
            if (groupPermIds.Any(id => !actor.Permissions.Any(p => p.RoleID == id)))
                return ServiceResult.Fail("You can only assign roles whose permissions you hold.");
        }

        if (await db.UserAssignedUserGroups.AnyAsync(a => a.UserId == userId && a.UserGroupId == roleId))
            return ServiceResult.Ok($"{target.FullName ?? target.Username} is already assigned to {roleGroup.GroupName}.");

        if (target.UserOrganisationId != orgId) target.UserOrganisationId = orgId;
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE,
            $"Added user {userId} to role {roleGroup.GroupName} (#{roleId})");
        db.UserAssignedUserGroups.Add(new UserAssignedUserGroup
        {
            UserId = userId,
            UserGroupId = roleId,
            OperationLogId = log.OperationLogId
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await contexts.InvalidateAsync(userId);
        return ServiceResult.Ok($"Added {target.FullName ?? target.Username} to {roleGroup.GroupName}.");
    }

    public async Task<ServiceResult> RemoveStaffFromRoleAsync(int roleId, int userId, int? organisationId)
    {
        var actor = await RequireAsync("Users.Roles.Edit", true);
        var org = await OrganisationAsync(actor, organisationId);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var orgId = await db.UserOrganisations.Where(x => x.BusinessEntityID == org).Select(x => x.UserOrganisationID).SingleAsync();
        var roleGroup = await db.UserGroups.AsNoTracking()
            .SingleOrDefaultAsync(g => g.UserGroupId == roleId && g.UserOrganisationId == orgId);
        if (roleGroup == null) return ServiceResult.Fail("Role was not found in this organisation.");

        var assignment = await db.UserAssignedUserGroups
            .SingleOrDefaultAsync(a => a.UserId == userId && a.UserGroupId == roleId);
        if (assignment == null) return ServiceResult.Ok("Staff member is not assigned to this role.");

        var targetName = await db.Users.AsNoTracking().Where(u => u.UserId == userId)
            .Select(u => u.FullName ?? u.Username).FirstOrDefaultAsync() ?? $"User #{userId}";
        _ = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE,
            $"Removed user {userId} from role {roleGroup.GroupName} (#{roleId})");
        db.UserAssignedUserGroups.Remove(assignment);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await contexts.InvalidateAsync(userId);
        return ServiceResult.Ok($"Removed {targetName} from {roleGroup.GroupName}.");
    }
}
