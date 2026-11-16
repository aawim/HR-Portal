using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class PermissionSettingsService
{
    private async Task<(UserContextDto Actor, int Org, int UserOrg)> RequireStaffAccessAdminAsync(bool allowSimulate = false)
    {
        var user = await access.RequireContextAsync();
        var allowed = user.IsSuperAdministrator ||
                      await access.HasPermissionAsync("Admin") ||
                      await access.HasPermissionAsync("Staff.AccessPolicy.Manage") ||
                      await access.HasPermissionAsync("Staff.AccessScope.Manage") ||
                      (allowSimulate && await access.HasPermissionAsync("Staff.AccessScope.Simulate"));
        if (!allowed)
            throw new UnauthorizedAccessException("You do not have permission to manage or simulate these access settings.");

        var org = await OrganisationAsync(user);
        await using var db = await factory.CreateDbContextAsync();
        var userOrg = await db.UserOrganisations.Where(x => x.BusinessEntityID == org).Select(x => x.UserOrganisationID).SingleAsync();
        return (user, org, userOrg);
    }

    public async Task<StaffAccessSettingsData> LoadStaffAccessAsync()
    {
        var (_, org, userOrg) = await RequireStaffAccessAdminAsync(allowSimulate: true);
        await using var db = await factory.CreateDbContextAsync();
        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == org)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? "Organisation";
        var data = new StaffAccessSettingsData { OrganisationId = org, OrganisationName = orgName };
        data.Users = await db.Users.AsNoTracking().Where(x => x.UserOrganisationId == userOrg && x.IsActive)
            .OrderBy(x => x.FullName).Select(x => new StaffAccessOption(x.UserId, x.FullName ?? x.Username)).ToListAsync();
        data.Groups = await db.UserGroups.AsNoTracking().Where(x => x.UserOrganisationId == userOrg)
            .OrderBy(x => x.GroupName).Select(x => new StaffAccessOption(x.UserGroupId, x.GroupName)).ToListAsync();
        var staff = await db.Individuals.AsNoTracking()
            .Where(individual => db.Jobs.Any(job => job.IndividualID == individual.BusinessEntityId &&
                job.OrganisationID == org && job.JobStateId == 4))
            .OrderBy(individual => individual.FirstNameEnglish)
            .ThenBy(individual => individual.LastNameEnglish)
            .Select(individual => new
            {
                Id = individual.BusinessEntityId,
                individual.FirstNameEnglish,
                individual.LastNameEnglish
            })
            .ToListAsync();
        data.Staff = staff.Select(individual => new StaffAccessOption(individual.Id,
            $"{individual.FirstNameEnglish} {individual.LastNameEnglish}".Trim())).ToList();
        data.Classifications = await db.PositionClassifications.AsNoTracking()
            .Where(x => x.IsActive != false && (x.OrganisationBusinessEntityID == null || x.OrganisationBusinessEntityID == org))
            .OrderBy(x => x.Name).Select(x => new StaffAccessOption(x.PositionClassificationId, x.Name)).ToListAsync();
        data.Units = await db.OrganisationStructures.AsNoTracking()
            .Where(x => x.OrganisationBusinessEntityID == org && x.OrganisationStructureStateId == 3)
            .OrderBy(x => x.Name)
            .Select(x => new StaffAccessOption(
                x.OrganisationStructureId,
                db.OrganisationStructureTypes.Where(t => t.OrganisationStructureTypeId == x.OrganisationStructureTypeId).Select(t => t.TypeName).FirstOrDefault() != null
                    ? $"{x.Name} ({db.OrganisationStructureTypes.Where(t => t.OrganisationStructureTypeId == x.OrganisationStructureTypeId).Select(t => t.TypeName).FirstOrDefault()})"
                    : x.Name))
            .ToListAsync();
        data.Teams = await db.Teams.AsNoTracking()
            .Where(x => x.OrganisationId == org && x.IsValid == true)
            .OrderBy(x => x.Name)
            .Select(x => new StaffAccessOption(x.TeamId, x.Name))
            .ToListAsync();
        data.Policies = await db.StaffAccessPolicies.AsNoTracking().Where(x => x.OrganisationId == org)
            .Select(x => new StaffAccessPolicyDto { Id = x.StaffAccessPolicyId, PositionClassificationId = x.PositionClassificationId,
                ClassificationName = db.PositionClassifications.Where(c => c.PositionClassificationId == x.PositionClassificationId).Select(c => c.Name).First(),
                IsRestricted = x.IsRestricted, IsActive = x.IsActive, Description = x.Description ?? "" }).ToListAsync();
        data.Scopes = await db.StaffAccessScopes.AsNoTracking().Where(x => x.OrganisationId == org)
            .Select(x => new StaffAccessScopeDto { Id = x.StaffAccessScopeId, SubjectType = x.UserId.HasValue ? "User" : "Role",
                SubjectId = x.UserId ?? x.UserGroupId!.Value,
                SubjectName = x.UserId.HasValue ? db.Users.Where(u => u.UserId == x.UserId).Select(u => u.FullName ?? u.Username).First()
                    : db.UserGroups.Where(g => g.UserGroupId == x.UserGroupId).Select(g => g.GroupName).First(),
                RequireExplicitGrant = x.RequireExplicitGrant, IsActive = x.IsActive }).ToListAsync();
        data.Grants = await db.StaffAccessGrants.AsNoTracking().Where(x => x.OrganisationId == org)
            .Select(x => new StaffAccessGrantDto { Id = x.StaffAccessGrantId, SubjectType = x.UserId.HasValue ? "User" : "Role",
                SubjectId = x.UserId ?? x.UserGroupId!.Value,
                SubjectName = x.UserId.HasValue ? db.Users.Where(u => u.UserId == x.UserId).Select(u => u.FullName ?? u.Username).First()
                    : db.UserGroups.Where(g => g.UserGroupId == x.UserGroupId).Select(g => g.GroupName).First(),
                ResourceType = x.TargetIndividualId.HasValue ? "Staff" : "Classification",
                ResourceId = x.TargetIndividualId ?? x.PositionClassificationId!.Value,
                ResourceName = x.TargetIndividualId.HasValue
                    ? db.Individuals.Where(i => i.BusinessEntityId == x.TargetIndividualId).Select(i => i.FirstNameEnglish + " " + (i.LastNameEnglish ?? "")).First()
                    : db.PositionClassifications.Where(c => c.PositionClassificationId == x.PositionClassificationId).Select(c => c.Name).First(),
                IsAllowed = x.IsAllowed, IsActive = x.IsActive, ExpiresAtUtc = x.ExpiresAtUtc, Reason = x.Reason ?? "" }).ToListAsync();

        data.OrganisationalScopes = await db.UserAccessScopes.AsNoTracking()
            .Where(x => x.OrganisationId == org)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new OrganisationalAccessScopeDto
            {
                Id = x.UserAccessScopeId,
                SubjectType = x.UserId.HasValue ? "User" : "Role",
                SubjectId = x.UserId ?? x.UserGroupId!.Value,
                SubjectName = x.UserId.HasValue
                    ? db.Users.Where(u => u.UserId == x.UserId).Select(u => u.FullName ?? u.Username).FirstOrDefault() ?? "User"
                    : db.UserGroups.Where(g => g.UserGroupId == x.UserGroupId).Select(g => g.GroupName).FirstOrDefault() ?? "Role",
                ScopeType = x.ScopeType,
                UnitId = x.UnitId,
                UnitName = x.UnitId.HasValue
                    ? db.OrganisationStructures.Where(u => u.OrganisationStructureId == x.UnitId).Select(u => u.Name).FirstOrDefault() ?? ""
                    : "",
                TeamId = x.TeamId,
                TeamName = x.TeamId.HasValue
                    ? db.Teams.Where(t => t.TeamId == x.TeamId).Select(t => t.Name).FirstOrDefault() ?? ""
                    : "",
                IncludeDescendants = x.IncludeDescendants,
                PermissionFilter = x.PermissionFilter ?? "",
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo,
                IsActive = x.IsActive,
                Reason = x.Reason ?? "",
                GrantedByName = x.GrantedByUserId.HasValue
                    ? db.Users.Where(u => u.UserId == x.GrantedByUserId).Select(u => u.FullName ?? u.Username).FirstOrDefault() ?? ""
                    : "System",
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        data.ScopeAudits = await db.UserAccessScopeAudits.AsNoTracking()
            .Where(x => x.OrganisationId == org)
            .OrderByDescending(x => x.TimestampUtc)
            .ThenByDescending(x => x.UserAccessScopeAuditId)
            .Take(100)
            .Select(x => new UserAccessScopeAuditDto
            {
                Id = x.UserAccessScopeAuditId,
                ScopeId = x.UserAccessScopeId,
                SubjectName = x.TargetUserId.HasValue
                    ? db.Users.Where(u => u.UserId == x.TargetUserId).Select(u => u.FullName ?? u.Username).FirstOrDefault() ?? "User"
                    : x.TargetUserGroupId.HasValue
                        ? db.UserGroups.Where(g => g.UserGroupId == x.TargetUserGroupId).Select(g => g.GroupName).FirstOrDefault() ?? "Role"
                        : "Subject",
                Action = x.Action,
                ScopeType = x.ScopeType,
                TargetUnitOrTeam = x.UnitId.HasValue
                    ? db.OrganisationStructures.Where(u => u.OrganisationStructureId == x.UnitId).Select(u => u.Name).FirstOrDefault() ?? $"Unit #{x.UnitId}"
                    : x.TeamId.HasValue
                        ? db.Teams.Where(t => t.TeamId == x.TeamId).Select(t => t.Name).FirstOrDefault() ?? $"Team #{x.TeamId}"
                        : x.ScopeType,
                IncludeDescendants = x.IncludeDescendants,
                PerformedByName = db.Users.Where(u => u.UserId == x.PerformedByUserId).Select(u => u.FullName ?? u.Username).FirstOrDefault() ?? $"User #{x.PerformedByUserId}",
                TimestampUtc = x.TimestampUtc,
                Detail = x.Detail ?? ""
            })
            .ToListAsync();

        return data;
    }

    public async Task<ServiceResult> SaveStaffAccessPolicyAsync(StaffAccessPolicyDto model)
    {
        var (actor, org, _) = await RequireStaffAccessAdminAsync();
        if (model.Description.Length > 1000) return ServiceResult.Fail("Description must be 1,000 characters or fewer.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.PositionClassifications.AnyAsync(x => x.PositionClassificationId == model.PositionClassificationId &&
            x.IsActive != false && (x.OrganisationBusinessEntityID == null || x.OrganisationBusinessEntityID == org)))
            return ServiceResult.Fail("Select a classification available to this organisation.");
        var entity = await db.StaffAccessPolicies.SingleOrDefaultAsync(x => x.OrganisationId == org && x.PositionClassificationId == model.PositionClassificationId)
            ?? new StaffAccessPolicy { OrganisationId = org, PositionClassificationId = model.PositionClassificationId };
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE,
            $"Updated staff ABAC classification {model.PositionClassificationId} for organisation {org}");
        if (entity.StaffAccessPolicyId == 0) db.StaffAccessPolicies.Add(entity);
        entity.IsRestricted = model.IsRestricted; entity.IsActive = model.IsActive;
        entity.Description = model.Description.Trim(); entity.OperationLogId = audit.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.StaffAccessPolicyId;
        return ServiceResult.Ok("Staff classification policy saved.");
    }

    public async Task<ServiceResult> SaveStaffAccessScopeAsync(StaffAccessScopeDto model)
    {
        var (_, org, userOrg) = await RequireStaffAccessAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = model.SubjectType == "User" ? model.SubjectId : (int?)null;
        var group = model.SubjectType == "Role" ? model.SubjectId : (int?)null;
        if (user.HasValue && !await db.Users.AnyAsync(x => x.UserId == user && x.UserOrganisationId == userOrg) ||
            group.HasValue && !await db.UserGroups.AnyAsync(x => x.UserGroupId == group && x.UserOrganisationId == userOrg))
            return ServiceResult.Fail("Select a user or role from this organisation.");
        var entity = model.Id == 0 ? new StaffAccessScope { OrganisationId = org } :
            await db.StaffAccessScopes.SingleOrDefaultAsync(x => x.StaffAccessScopeId == model.Id && x.OrganisationId == org);
        if (entity == null) return ServiceResult.Fail("Access scope was not found.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE, $"Updated staff ABAC scope for organisation {org}");
        if (model.Id == 0) db.StaffAccessScopes.Add(entity);
        entity.UserId = user; entity.UserGroupId = group; entity.RequireExplicitGrant = model.RequireExplicitGrant;
        entity.IsActive = model.IsActive; entity.OperationLogId = audit.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.StaffAccessScopeId;
        return ServiceResult.Ok("Staff access scope saved.");
    }

    public async Task<ServiceResult> SaveStaffAccessGrantAsync(StaffAccessGrantDto model)
    {
        var (_, org, userOrg) = await RequireStaffAccessAdminAsync();
        if (string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Length > 1000) return ServiceResult.Fail("Enter a reason of up to 1,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = model.SubjectType == "User" ? model.SubjectId : (int?)null;
        var group = model.SubjectType == "Role" ? model.SubjectId : (int?)null;
        var staff = model.ResourceType == "Staff" ? model.ResourceId : (int?)null;
        var classification = model.ResourceType == "Classification" ? model.ResourceId : (int?)null;
        if (user.HasValue && !await db.Users.AnyAsync(x => x.UserId == user && x.UserOrganisationId == userOrg) ||
            group.HasValue && !await db.UserGroups.AnyAsync(x => x.UserGroupId == group && x.UserOrganisationId == userOrg) ||
            staff.HasValue && !await db.Jobs.AnyAsync(x => x.IndividualID == staff && x.OrganisationID == org) ||
            classification.HasValue && !await db.PositionClassifications.AnyAsync(x => x.PositionClassificationId == classification &&
                (x.OrganisationBusinessEntityID == null || x.OrganisationBusinessEntityID == org)))
            return ServiceResult.Fail("Select a valid subject and staff resource from this organisation.");
        var entity = model.Id == 0 ? new StaffAccessGrant { OrganisationId = org } :
            await db.StaffAccessGrants.SingleOrDefaultAsync(x => x.StaffAccessGrantId == model.Id && x.OrganisationId == org);
        if (entity == null) return ServiceResult.Fail("Access grant was not found.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE, $"Updated staff ABAC grant for organisation {org}: {model.Reason.Trim()}");
        if (model.Id == 0) db.StaffAccessGrants.Add(entity);
        entity.UserId = user; entity.UserGroupId = group; entity.TargetIndividualId = staff; entity.PositionClassificationId = classification;
        entity.IsAllowed = model.IsAllowed; entity.IsActive = model.IsActive; entity.ExpiresAtUtc = model.ExpiresAtUtc;
        entity.Reason = model.Reason.Trim(); entity.OperationLogId = audit.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.StaffAccessGrantId;
        return ServiceResult.Ok("Staff access grant saved.");
    }

    public async Task<ServiceResult> SaveOrganisationalAccessScopeAsync(OrganisationalAccessScopeDto model)
    {
        var (actor, org, userOrg) = await RequireStaffAccessAdminAsync();
        if (!AccessScopeTypes.IsValid(model.ScopeType))
            return ServiceResult.Fail("Select a valid organisational access scope type.");
        if (string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length > 1000)
            return ServiceResult.Fail("Enter an authorisation reason of up to 1,000 characters.");
        if (model.PermissionFilter?.Length > 200)
            return ServiceResult.Fail("Permission filter must be 200 characters or fewer.");

        var scopeType = AccessScopeTypes.Normalize(model.ScopeType);
        var validFrom = model.ValidFrom == default ? DateTime.UtcNow.Date : model.ValidFrom;
        if (model.ValidTo.HasValue && model.ValidTo.Value <= validFrom)
            return ServiceResult.Fail("Expiry date/time must be after the start date.");

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var user = model.SubjectType == "User" ? model.SubjectId : (int?)null;
        var group = model.SubjectType == "Role" ? model.SubjectId : (int?)null;
        if ((!user.HasValue && !group.HasValue) ||
            (user.HasValue && !await db.Users.AnyAsync(x => x.UserId == user && x.UserOrganisationId == userOrg)) ||
            (group.HasValue && !await db.UserGroups.AnyAsync(x => x.UserGroupId == group && x.UserOrganisationId == userOrg)))
            return ServiceResult.Fail("Select a valid user or role from this organisation.");

        if (user.HasValue && user.Value == actor.UserId && !actor.IsSuperAdministrator)
            return ServiceResult.Fail("You cannot grant or expand your own organisational access scope.");

        int? unitId = null;
        int? teamId = null;
        if (scopeType is AccessScopeTypes.Division or AccessScopeTypes.DivisionAndDescendants or AccessScopeTypes.Department or AccessScopeTypes.Custom)
        {
            if (!model.UnitId.HasValue || model.UnitId.Value <= 0 ||
                !await db.OrganisationStructures.AnyAsync(s => s.OrganisationStructureId == model.UnitId.Value &&
                                                               s.OrganisationBusinessEntityID == org &&
                                                               s.OrganisationStructureStateId == 3))
                return ServiceResult.Fail("Select an approved division, department, or organisational unit in this organisation.");
            unitId = model.UnitId.Value;
        }
        else if (scopeType == AccessScopeTypes.Team && model.TeamId is > 0)
        {
            if (!await db.Teams.AnyAsync(t => t.TeamId == model.TeamId.Value && t.OrganisationId == org && t.IsValid == true))
                return ServiceResult.Fail("Select an active team in this organisation.");
            teamId = model.TeamId.Value;
        }

        var isNew = model.Id == 0;
        var entity = isNew
            ? new UserAccessScope { OrganisationId = org, CreatedAtUtc = DateTime.UtcNow, GrantedByUserId = actor.UserId }
            : await db.UserAccessScopes.SingleOrDefaultAsync(x => x.UserAccessScopeId == model.Id && x.OrganisationId == org);

        if (entity is null)
            return ServiceResult.Fail("Organisational access scope was not found.");

        var includeDescendants = scopeType == AccessScopeTypes.DivisionAndDescendants ||
                                 (scopeType is AccessScopeTypes.Division or AccessScopeTypes.Department or AccessScopeTypes.Team or AccessScopeTypes.Custom && model.IncludeDescendants) ||
                                 scopeType == AccessScopeTypes.Organisation;

        var auditLog = await logs.CreateAsync(
            db,
            SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE,
            $"Saved organisational access scope ({scopeType}) for organisation {org}: {model.Reason.Trim()}");

        if (isNew) db.UserAccessScopes.Add(entity);
        entity.UserId = user;
        entity.UserGroupId = group;
        entity.ScopeType = scopeType;
        entity.UnitId = unitId;
        entity.TeamId = teamId;
        entity.IncludeDescendants = includeDescendants;
        entity.PermissionFilter = string.IsNullOrWhiteSpace(model.PermissionFilter) ? null : model.PermissionFilter.Trim();
        entity.ValidFrom = validFrom;
        entity.ValidTo = model.ValidTo;
        entity.IsActive = model.IsActive;
        entity.Reason = model.Reason.Trim();
        entity.OperationLogId = auditLog.OperationLogId;

        await db.SaveChangesAsync();

        var auditAction = !model.IsActive
            ? AccessScopeAuditActions.Revoked
            : model.ValidTo.HasValue
                ? AccessScopeAuditActions.TemporaryElevation
                : isNew
                    ? AccessScopeAuditActions.Granted
                    : AccessScopeAuditActions.Updated;

        db.UserAccessScopeAudits.Add(new UserAccessScopeAudit
        {
            UserAccessScopeId = entity.UserAccessScopeId,
            OrganisationId = org,
            TargetUserId = user,
            TargetUserGroupId = group,
            Action = auditAction,
            ScopeType = scopeType,
            UnitId = unitId,
            TeamId = teamId,
            IncludeDescendants = includeDescendants,
            ValidFrom = validFrom,
            ValidTo = model.ValidTo,
            PerformedByUserId = actor.UserId,
            TimestampUtc = DateTime.UtcNow,
            Detail = model.Reason.Trim(),
            OperationLogId = auditLog.OperationLogId
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        model.Id = entity.UserAccessScopeId;

        if (user.HasValue)
        {
            await contexts.InvalidateAsync(user.Value);
        }
        else if (group.HasValue)
        {
            var memberIds = await db.UserAssignedUserGroups
                .Where(x => x.UserGroupId == group.Value)
                .Select(x => x.UserId)
                .ToListAsync();
            foreach (var uid in memberIds)
                await contexts.InvalidateAsync(uid);
        }

        return ServiceResult.Ok("Organisational access scope saved.");
    }

    public async Task<ServiceResult> RevokeOrganisationalAccessScopeAsync(int scopeId, string reason)
    {
        var (actor, org, _) = await RequireStaffAccessAdminAsync();
        var cleanReason = string.IsNullOrWhiteSpace(reason) ? "Revoked by administrator." : reason.Trim();
        if (cleanReason.Length > 1000)
            return ServiceResult.Fail("Revocation reason must be 1,000 characters or fewer.");

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var entity = await db.UserAccessScopes
            .SingleOrDefaultAsync(x => x.UserAccessScopeId == scopeId && x.OrganisationId == org);
        if (entity is null)
            return ServiceResult.Fail("Organisational access scope was not found.");

        var auditLog = await logs.CreateAsync(
            db,
            SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE,
            $"Revoked organisational access scope #{scopeId} in organisation {org}: {cleanReason}");

        entity.IsActive = false;
        entity.RevokedByUserId = actor.UserId;
        entity.RevokedAtUtc = DateTime.UtcNow;
        entity.RevocationReason = cleanReason;
        entity.OperationLogId = auditLog.OperationLogId;

        db.UserAccessScopeAudits.Add(new UserAccessScopeAudit
        {
            UserAccessScopeId = entity.UserAccessScopeId,
            OrganisationId = org,
            TargetUserId = entity.UserId,
            TargetUserGroupId = entity.UserGroupId,
            Action = AccessScopeAuditActions.Revoked,
            ScopeType = entity.ScopeType,
            UnitId = entity.UnitId,
            TeamId = entity.TeamId,
            IncludeDescendants = entity.IncludeDescendants,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            PerformedByUserId = actor.UserId,
            TimestampUtc = DateTime.UtcNow,
            Detail = cleanReason,
            OperationLogId = auditLog.OperationLogId
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        if (entity.UserId.HasValue)
            await contexts.InvalidateAsync(entity.UserId.Value);

        return ServiceResult.Ok("Organisational access scope revoked.");
    }

    public async Task<EffectiveAccessSimulationDto> SimulateEffectiveAccessAsync(int userId)
    {
        var (_, org, userOrg) = await RequireStaffAccessAdminAsync(allowSimulate: true);
        await using var db = await factory.CreateDbContextAsync();

        var targetUser = await db.Users.AsNoTracking()
            .Where(u => u.UserId == userId && u.UserOrganisationId == userOrg)
            .Select(u => new { u.UserId, u.Username, u.FullName, u.BusinessEntityID })
            .SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Select a user account belonging to this organisation.");

        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == org)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? "Organisation";

        var today = HRM.Helpers.OrganisationalAccessScopeResolver.TodayMaldives;
        var resolved = await HRM.Helpers.OrganisationalAccessScopeResolver.ResolveForUserAsync(db, userId, org);

        var activeJobInfo = targetUser.BusinessEntityID > 0
            ? await (
                from j in db.Jobs.AsNoTracking()
                join s in db.OrganisationStructures.AsNoTracking()
                    on j.OrganisationStructureId equals s.OrganisationStructureId into sg
                from structure in sg.DefaultIfEmpty()
                where j.IndividualID == targetUser.BusinessEntityID &&
                      j.OrganisationID == org &&
                      j.JobStateId == SharedConfig.JobStates.APPROVED &&
                      j.JoinedDate <= today &&
                      (!j.TerminatedDate.HasValue || j.TerminatedDate > today)
                orderby j.JoinedDate descending, j.JobId descending
                select new
                {
                    j.JobId,
                    j.OrganisationStructureId,
                    StructureName = structure != null ? structure.Name : "Unassigned",
                    PositionName = (
                        from jp in db.JobPositions.AsNoTracking()
                        join p in db.Positions.AsNoTracking() on jp.PositionId equals p.PositionId
                        where jp.JobId == j.JobId && jp.JobPositionStateId == 4 &&
                              jp.FromDate <= today && (!jp.ToDate.HasValue || jp.ToDate > today)
                        orderby jp.FromDate descending
                        select p.Name).FirstOrDefault()
                        ?? db.JobTypes.Where(jt => jt.JobTypeId == j.JobTypeId).Select(jt => jt.TypeName).FirstOrDefault()
                        ?? "Employee"
                }).FirstOrDefaultAsync()
            : null;

        var structures = await (
            from s in db.OrganisationStructures.AsNoTracking()
            where s.OrganisationBusinessEntityID == org && s.OrganisationStructureStateId == 3
            join r in db.OrganisationStructureRelations.AsNoTracking().Where(rel => rel.IsCurrent)
                on s.OrganisationStructureId equals r.OrganisationStructureId into rg
            from rel in rg.DefaultIfEmpty()
            orderby s.Name
            select new
            {
                s.OrganisationStructureId,
                s.Name,
                TypeName = db.OrganisationStructureTypes
                    .Where(t => t.OrganisationStructureTypeId == s.OrganisationStructureTypeId)
                    .Select(t => t.TypeName)
                    .FirstOrDefault() ?? "Unit",
                ParentId = rel != null ? (int?)rel.ParentOrganisationStructureId : null
            }).ToListAsync();

        var activeJobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == org &&
                        j.JobStateId == SharedConfig.JobStates.APPROVED &&
                        j.JoinedDate <= today &&
                        (!j.TerminatedDate.HasValue || j.TerminatedDate > today))
            .Select(j => new { j.IndividualID, j.OrganisationStructureId })
            .ToListAsync();

        var allStaffIds = activeJobs.Select(j => j.IndividualID).Distinct().ToArray();
        var visibleStaffSet = await HRM.Helpers.OrganisationalAccessScopeResolver.FilterAuthorisedEmployeesAsync(
            db, resolved, allStaffIds);

        var unitMatrix = new List<EffectiveUnitAccessPreviewDto>();
        foreach (var unit in structures)
        {
            var unitStaffIds = activeJobs
                .Where(j => j.OrganisationStructureId == unit.OrganisationStructureId)
                .Select(j => j.IndividualID)
                .Distinct()
                .ToArray();

            var visibleInUnit = unitStaffIds.Count(id => visibleStaffSet.Contains(id));
            var directUnitAccess = resolved.CanAccessUnit(unit.OrganisationStructureId);
            var isHomeUnit = activeJobInfo?.OrganisationStructureId == unit.OrganisationStructureId;

            string accessLevel;
            string summary;
            bool isAllowed;

            if (directUnitAccess)
            {
                isAllowed = true;
                accessLevel = "Full";
                summary = resolved.HasFullOrganisationScope
                    ? "Allowed (Organisation-wide scope)"
                    : "Allowed (Authorised unit / descendant scope)";
            }
            else if (visibleInUnit > 0 && (!isHomeUnit || visibleInUnit > 1))
            {
                isAllowed = true;
                accessLevel = "Team scope";
                summary = $"Team scope ({visibleInUnit} of {unitStaffIds.Length} staff visible)";
            }
            else if (isHomeUnit && targetUser.BusinessEntityID > 0)
            {
                isAllowed = true;
                accessLevel = "Own permitted data";
                summary = "Own permitted data (Self scope only)";
            }
            else
            {
                isAllowed = false;
                accessLevel = "Restricted";
                summary = "Restricted (Outside authorised scope)";
            }

            unitMatrix.Add(new EffectiveUnitAccessPreviewDto
            {
                UnitId = unit.OrganisationStructureId,
                UnitName = unit.Name,
                UnitType = unit.TypeName,
                ParentUnitId = unit.ParentId,
                IsAllowed = isAllowed,
                AccessLevel = accessLevel,
                AccessSummary = summary,
                TotalStaffCount = unitStaffIds.Length,
                VisibleStaffCount = visibleInUnit
            });
        }

        var activeScopesDto = resolved.ActiveGrants.Select(x => new OrganisationalAccessScopeDto
        {
            Id = x.UserAccessScopeId,
            SubjectType = x.UserId.HasValue ? "User" : "Role",
            SubjectId = x.UserId ?? x.UserGroupId ?? 0,
            ScopeType = x.ScopeType,
            UnitId = x.UnitId,
            UnitName = x.UnitId.HasValue
                ? structures.FirstOrDefault(s => s.OrganisationStructureId == x.UnitId.Value)?.Name ?? $"Unit #{x.UnitId}"
                : "",
            TeamId = x.TeamId,
            IncludeDescendants = x.IncludeDescendants,
            PermissionFilter = x.PermissionFilter ?? "",
            ValidFrom = x.ValidFrom,
            ValidTo = x.ValidTo,
            IsActive = x.IsActive,
            Reason = x.Reason ?? ""
        }).ToList();

        string scopeHeadline;
        if (resolved.HasFullOrganisationScope)
        {
            scopeHeadline = $"Entire Organisation ({orgName})";
        }
        else if (activeScopesDto.Count > 0)
        {
            scopeHeadline = string.Join(" + ", activeScopesDto.Select(s =>
                !string.IsNullOrWhiteSpace(s.UnitName)
                    ? (s.IncludeDescendants ? $"{s.UnitName} (+ child units)" : s.UnitName)
                    : s.ScopeType).Distinct());
        }
        else if (resolved.AuthorisedStructureIds.Count > 0 || resolved.AuthorisedTeamIds.Count > 0)
        {
            var unitNames = structures
                .Where(s => resolved.AuthorisedStructureIds.Contains(s.OrganisationStructureId))
                .Select(s => s.Name)
                .Take(3)
                .ToList();
            scopeHeadline = unitNames.Count > 0
                ? $"Supervised Scope ({string.Join(", ", unitNames)})"
                : "Supervised Team Scope";
        }
        else
        {
            scopeHeadline = "Self (Own Permitted Data Only)";
        }

        var fp = resolved.FieldPermissions;
        var fieldMatrix = new List<EffectiveFieldAccessPreviewDto>
        {
            new()
            {
                FieldCategory = "Public Directory Profile",
                PermissionCode = HRM.Authorization.Permissions.DirectoryPublicProfileView,
                IsAllowed = fp.CanViewPublicProfile,
                StatusLabel = fp.CanViewPublicProfile ? "Allowed" : "Restricted"
            },
            new()
            {
                FieldCategory = "Basic Employment & Position Details",
                PermissionCode = HRM.Authorization.Permissions.EmployeeView,
                IsAllowed = fp.CanViewBasicEmployment,
                StatusLabel = fp.CanViewBasicEmployment ? "Allowed" : "Restricted"
            },
            new()
            {
                FieldCategory = "Private HR Records & Official Documents",
                PermissionCode = HRM.Authorization.Permissions.EmployeePrivateRecordView,
                IsAllowed = fp.CanViewPrivateRecord,
                StatusLabel = fp.CanViewPrivateRecord ? "Allowed" : "Restricted"
            },
            new()
            {
                FieldCategory = "National ID, Passport & Emergency Contacts",
                PermissionCode = HRM.Authorization.Permissions.EmployeeIdentificationView,
                IsAllowed = fp.CanViewIdentification,
                StatusLabel = fp.CanViewIdentification ? "Allowed" : "Restricted"
            },
            new()
            {
                FieldCategory = "Salary, Compensation & Bank Accounts",
                PermissionCode = HRM.Authorization.Permissions.EmployeeSalaryView,
                IsAllowed = fp.CanViewSalaryAndBank,
                StatusLabel = fp.CanViewSalaryAndBank ? "Allowed" : "Restricted"
            },
            new()
            {
                FieldCategory = "Disciplinary Records",
                PermissionCode = HRM.Authorization.Permissions.EmployeeDisciplinaryView,
                IsAllowed = fp.CanViewDisciplinary,
                StatusLabel = fp.CanViewDisciplinary ? "Allowed" : "Restricted"
            },
            new()
            {
                FieldCategory = "Medical & Health Information",
                PermissionCode = HRM.Authorization.Permissions.EmployeeMedicalView,
                IsAllowed = fp.CanViewMedical,
                StatusLabel = fp.CanViewMedical ? "Allowed" : "Restricted"
            },
            new()
            {
                FieldCategory = "Intranet Public Announcements",
                PermissionCode = HRM.Authorization.Permissions.IntranetAnnouncementView,
                IsAllowed = fp.CanViewIntranetAnnouncements,
                StatusLabel = fp.CanViewIntranetAnnouncements ? "Allowed" : "Restricted"
            }
        };

        var directPerms = await db.UserRoles.AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive && r.UserOrganisationId == userOrg)
            .Select(r => r.Role.RoleKey)
            .ToListAsync();
        var groupPerms = await db.UserAssignedUserGroups.AsNoTracking()
            .Where(g => g.UserId == userId && (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisationId == userOrg))
            .SelectMany(g => g.UserGroup.UserGroupRoles.Where(r => r.IsActive).Select(r => r.Role.RoleKey))
            .ToListAsync();

        var effectivePerms = directPerms.Concat(groupPerms)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k)
            .ToList();

        return new EffectiveAccessSimulationDto
        {
            UserId = targetUser.UserId,
            UserName = targetUser.FullName ?? targetUser.Username,
            JobTitle = activeJobInfo?.PositionName ?? "No Active Position",
            HomeUnitName = activeJobInfo?.StructureName ?? "No Assigned Unit",
            OrganisationName = orgName,
            EffectiveScopeSummary = scopeHeadline,
            HasFullOrganisationScope = resolved.HasFullOrganisationScope,
            TotalOrganisationStaff = allStaffIds.Length,
            TotalVisibleStaff = visibleStaffSet.Count,
            ActiveScopes = activeScopesDto,
            UnitAccessMatrix = unitMatrix,
            FieldAccessMatrix = fieldMatrix,
            EffectivePermissions = effectivePerms
        };
    }
}
