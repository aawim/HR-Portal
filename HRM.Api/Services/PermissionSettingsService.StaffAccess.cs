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
    private async Task<(UserContextDto Actor, int Org, int UserOrg)> RequireStaffAccessAdminAsync()
    {
        var actor = await RequireAsync("Staff.AccessPolicy.Manage", false);
        var org = await OrganisationAsync(actor);
        await using var db = await factory.CreateDbContextAsync();
        var userOrg = await db.UserOrganisations.Where(x => x.BusinessEntityID == org).Select(x => x.UserOrganisationID).SingleAsync();
        return (actor, org, userOrg);
    }

    public async Task<StaffAccessSettingsData> LoadStaffAccessAsync()
    {
        var (_, org, userOrg) = await RequireStaffAccessAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        var data = new StaffAccessSettingsData { OrganisationId = org };
        data.Users = await db.Users.AsNoTracking().Where(x => x.UserOrganisationId == userOrg && x.IsActive)
            .OrderBy(x => x.FullName).Select(x => new StaffAccessOption(x.UserId, x.FullName ?? x.Username)).ToListAsync();
        data.Groups = await db.UserGroups.AsNoTracking().Where(x => x.UserOrganisationId == userOrg)
            .OrderBy(x => x.GroupName).Select(x => new StaffAccessOption(x.UserGroupId, x.GroupName)).ToListAsync();
        data.Staff = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == org && x.JobStateId == 4)
            .Select(x => new StaffAccessOption(x.IndividualID,
                x.Individual.FirstNameEnglish + " " + (x.Individual.LastNameEnglish ?? "")))
            .Distinct().OrderBy(x => x.Name).ToListAsync();
        data.Classifications = await db.PositionClassifications.AsNoTracking()
            .Where(x => x.IsActive != false && (x.OrganisationBusinessEntityID == null || x.OrganisationBusinessEntityID == org))
            .OrderBy(x => x.Name).Select(x => new StaffAccessOption(x.PositionClassificationId, x.Name)).ToListAsync();
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
}
