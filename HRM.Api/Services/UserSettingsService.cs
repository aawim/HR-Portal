using System.ComponentModel.DataAnnotations;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class UserSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService logs, IUserContextService contexts)
:global::HRM.Services.Contracts.UserSettingsServiceContracts,global::HRM.Services.Contracts.UserSettingsServiceContracts.IOperations{
    private async Task<(UserContextDto User, int Org)> RequireAsync(params string[] permissions)
    {
        var user = await access.RequireContextAsync();
        var allowed = user.IsSuperAdministrator || await access.HasPermissionAsync("Admin");
        foreach (var key in permissions) allowed |= await access.HasPermissionAsync(key);
        if (!allowed) throw new UnauthorizedAccessException("You do not have permission to manage these user settings.");
        var org = user.Organisation?.OrganisationBusinessEntityId ?? 0;
        if (org == 0 && user.ActiveJob != null)
        {
            await using var db = await factory.CreateDbContextAsync();
            org = await db.Jobs.Where(x => x.JobId == user.ActiveJob.JobId).Select(x => x.OrganisationID).SingleAsync();
        }
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select an organisation you can manage.");
        return (user, org);
    }

    public async Task<UserSettingsData> LoadAsync(UserSettingsTab tab)
    {
        var required = tab switch
        {
            UserSettingsTab.Accounts => new[] { "Users.View" }, UserSettingsTab.Permissions => new[] { "Users.Roles.View", "Users.Roles.Edit" },
            UserSettingsTab.Staff => new[] { "Staff.Search", "Staff.Search.Admin", "Staff.Admin.Edit" },
            UserSettingsTab.Enrollment => new[] { "Staff.EnrolmentNumbers.Manage" }, _ => new[] { "UserOrganisations.View", "UserOrganisations.Admin.View" }
        };
        var (_, org) = await RequireAsync(required);
        await using var db = await factory.CreateDbContextAsync();
        var organisation = await db.UserOrganisations.AsNoTracking().Where(x => x.BusinessEntityID == org)
            .Select(x => new { x.UserOrganisationID, x.Organisation.OrganisationName }).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("This organisation has no user organisation configuration.");
        var result = new UserSettingsData { UserOrganisationId = organisation.UserOrganisationID, OrganisationName = organisation.OrganisationName };
        if (tab is UserSettingsTab.Accounts or UserSettingsTab.Permissions)
        {
            result.Users = await db.Users.AsNoTracking().AsSplitQuery().Where(x => x.UserOrganisationId == organisation.UserOrganisationID)
                .OrderBy(x => x.FullName).Select(x => new ManagedUserDto
                {
                    Id = x.UserId, Username = x.Username, Name = x.FullName ?? x.Username, Email = x.Email, Mobile = x.Mobile,
                    Active = x.IsActive, Approved = x.IsApproved == true, StateId = x.LocalUserStateId, SsoStatus = x.SsouserState.StateName,
                    DirectPermissionIds = x.UserRoles.Where(r => r.UserOrganisationId == organisation.UserOrganisationID && r.IsActive).Select(r => r.RoleId).ToList(),
                    RoleIds = x.UserAssignedUserGroups.Where(g => g.UserGroup.UserOrganisationId == organisation.UserOrganisationID).Select(g => g.UserGroupId).ToList()
                }).ToListAsync();
            result.AccountStates = await db.LocalUserStates.AsNoTracking().Select(x => new PermissionSettingOption(x.LocalUserStateId, x.StateName)).ToListAsync();
        }
        if (tab is UserSettingsTab.Permissions or UserSettingsTab.Organisation)
        {
            result.Permissions = await db.Roles.AsNoTracking().OrderBy(x => x.RoleKey).Select(x => new PermissionDefinitionDto
            { Id = x.RoleID, Key = x.RoleKey, Name = x.Name, Description = x.Description ?? "", ContextId = x.ContextId }).ToListAsync();
            result.Roles = await db.UserGroups.AsNoTracking().Where(x => x.UserOrganisationId == organisation.UserOrganisationID)
                .OrderBy(x => x.GroupName).Select(x => new AccessRoleDto { Id = x.UserGroupId, Name = x.GroupName,
                    PermissionIds = x.UserGroupRoles.Where(p => p.IsActive).Select(p => p.RoleId).ToList() }).ToListAsync();
            result.OrganisationPermissionIds = await db.UserOrganisationRoles.AsNoTracking()
                .Where(x => x.UserOrganisationID == organisation.UserOrganisationID && x.IsActive).Select(x => x.RoleID).ToListAsync();
        }
        if (tab is UserSettingsTab.Staff or UserSettingsTab.Enrollment)
        {
            result.People = await db.Individuals.AsNoTracking().Where(x => db.Jobs.Any(j => j.IndividualID == x.BusinessEntityId && j.OrganisationID == org) ||
                db.Users.Any(u => u.BusinessEntityID == x.BusinessEntityId && u.UserOrganisationId == organisation.UserOrganisationID))
                .OrderBy(x => x.FirstNameEnglish).Select(x => new ManagedStaffDto
                { IndividualId = x.BusinessEntityId, Name = x.FirstNameEnglish + " " + (x.MiddleNameEnglish ?? "") + " " + (x.LastNameEnglish ?? "") }).ToListAsync();
            var ids = result.People.Select(x => x.IndividualId).ToArray();
            result.Staff = await db.Staffs.AsNoTracking().Where(x => ids.Contains(x.IndividualId)).OrderBy(x => x.Individual.FirstNameEnglish)
                .Select(x => new ManagedStaffDto { IndividualId = x.IndividualId, Name = x.Individual.FirstNameEnglish + " " + (x.Individual.LastNameEnglish ?? ""), EmployeeNumber = x.EmployeeNumber }).ToListAsync();
            result.Enrollments = await db.StaffEnrollmentNumbers.AsNoTracking().Where(x => x.OrganisationId == org)
                .OrderBy(x => x.Individual.Individual.FirstNameEnglish).Select(x => new ManagedEnrollmentDto
                { Id = x.StaffEnrollmentNumberId, IndividualId = x.IndividualId, Name = x.Individual.Individual.FirstNameEnglish + " " + (x.Individual.Individual.LastNameEnglish ?? ""), Number = x.EnrollmentNumber.Trim() }).ToListAsync();
        }
        return result;
    }

    private static async Task<User> ScopedUserAsync(HrmTeContext db, int id, int org) => await db.Users
        .Include(x => x.UserRoles).Include(x => x.UserAssignedUserGroups).ThenInclude(x => x.UserGroup)
        .SingleOrDefaultAsync(x => x.UserId == id && x.UserOrganisation!.BusinessEntityID == org)
        ?? throw new InvalidOperationException("Account was not found in this organisation.");

    public async Task<ServiceResult> SaveAccountAsync(ManagedUserDto model)
    {
        var (actor, org) = await RequireAsync("Users.Edit");
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Length > 255 || model.Email.Length > 128 ||
            !new EmailAddressAttribute().IsValid(model.Email) || model.Mobile?.Length > 20)
            return ServiceResult.Fail("Enter a name, valid email, and mobile number of up to 20 characters.");
        if (actor.UserId == model.Id && (!model.Active || !model.Approved || model.StateId != 1))
            return ServiceResult.Fail("You cannot suspend or disable your own account here.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity = await ScopedUserAsync(db, model.Id, org);
        if (!await db.LocalUserStates.AnyAsync(x => x.LocalUserStateId == model.StateId)) return ServiceResult.Fail("Select a valid account state.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE, $"Updated account {entity.UserId}");
        entity.FullName = model.Name.Trim(); entity.Email = model.Email.Trim(); entity.Mobile = model.Mobile?.Trim();
        entity.IsActive = model.Active; entity.IsApproved = model.Approved; entity.LocalUserStateId = model.StateId;
        entity.LastUpdatedByUserId = actor.UserId; entity.LastUpdatedDate = DateTime.Now; entity.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); await contexts.InvalidateAsync(entity.UserId);
        return ServiceResult.Ok("Account saved.");
    }

    public async Task<ServiceResult> SaveLegacyUserAsync(User model)
    {
        var (_, org) = await RequireAsync("Users.Edit");
        await using var db = await factory.CreateDbContextAsync();
        var current = await ScopedUserAsync(db, model.UserId, org);
        if (model.Username != current.Username || model.BusinessEntityID != current.BusinessEntityID)
            return ServiceResult.Fail("Account identifiers and employee identity cannot be changed through account editing.");
        return await SaveAccountAsync(new ManagedUserDto { Id = current.UserId, Name = model.FullName ?? current.FullName ?? current.Username,
            Email = model.Email ?? current.Email, Mobile = model.Mobile, Active = model.IsActive, Approved = model.IsApproved == true, StateId = model.LocalUserStateId });
    }

    public async Task<ServiceResult> SaveLegacyProfileAsync(UserProfileDto model)
    {
        var (_, org) = await RequireAsync("Users.Edit");
        await using var db = await factory.CreateDbContextAsync();
        var current = await ScopedUserAsync(db, model.UserId, org);
        if (model.Username != current.Username || model.IndividualID != current.BusinessEntityID)
            return ServiceResult.Fail("Account identifiers and employee identity cannot be changed through profile editing.");
        return await SaveAccountAsync(new ManagedUserDto { Id = current.UserId, Name = current.FullName ?? current.Username,
            Email = string.IsNullOrWhiteSpace(model.Email) ? current.Email : model.Email, Mobile = model.Mobile,
            Active = current.IsActive, Approved = current.IsApproved == true, StateId = current.LocalUserStateId });
    }

    private async Task ValidateGrantsAsync(HrmTeContext db, UserContextDto actor, IEnumerable<int> desired, IEnumerable<int> existing)
    {
        var ids = desired.Distinct().ToArray();
        if (await db.Roles.CountAsync(x => ids.Contains(x.RoleID)) != ids.Length) throw new InvalidOperationException("A selected permission no longer exists.");
        if (!actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") && ids.Except(existing).Any(id => !actor.Permissions.Any(p => p.RoleID == id)))
            throw new UnauthorizedAccessException("You can only grant permissions you hold.");
    }

    public async Task<ServiceResult> SaveUserAccessAsync(ManagedUserDto model)
    {
        var (actor, org) = await RequireAsync("Users.Roles.Edit");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity = await ScopedUserAsync(db, model.Id, org); var orgId = entity.UserOrganisationId!.Value;
        var allowedIds = await db.UserOrganisationRoles.Where(p => p.UserOrganisationID == orgId && p.IsActive).Select(p => p.RoleID).ToListAsync();
        if (model.DirectPermissionIds.Except(allowedIds).Any()) return ServiceResult.Fail("Some direct permissions are not enabled for this organisation. Update Organisation Permissions first.");
        var desiredGroups = model.RoleIds.Distinct().ToArray();
        var groups = await db.UserGroups.Include(x => x.UserGroupRoles).Where(x => desiredGroups.Contains(x.UserGroupId) && x.UserOrganisationId == orgId).ToListAsync();
        if (groups.Count != desiredGroups.Length) return ServiceResult.Fail("Select roles from the current organisation.");
        var oldDirect = entity.UserRoles.Where(x => x.UserOrganisationId == orgId && x.IsActive).Select(x => x.RoleId).ToArray();
        var oldGroups = entity.UserAssignedUserGroups.Where(x => x.UserGroup.UserOrganisationId == orgId).Select(x => x.UserGroupId).ToArray();
        var addedGroupPermissions = groups.Where(x => !oldGroups.Contains(x.UserGroupId)).SelectMany(x => x.UserGroupRoles.Where(r => r.IsActive).Select(r => r.RoleId));
        await ValidateGrantsAsync(db, actor, model.DirectPermissionIds.Concat(addedGroupPermissions), oldDirect);
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE, $"Updated permissions and roles for account {entity.UserId}");
        await UserAccessAssignmentWriter.ApplyAsync(db, entity, desiredGroups, model.DirectPermissionIds, log.OperationLogId);
        await db.SaveChangesAsync(); await tx.CommitAsync(); await contexts.InvalidateAsync(entity.UserId);
        return ServiceResult.Ok("Account roles and direct permissions saved.");
    }

    public async Task<ServiceResult> SaveStaffAsync(ManagedStaffDto model)
    {
        var (_, org) = await RequireAsync("Staff.Admin.Edit", "Staff.Add.Admin", "Staff.Add");
        if (model.EmployeeNumber?.Length > 50) return ServiceResult.Fail("Employee number must be 50 characters or fewer.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Individuals.AnyAsync(x => x.BusinessEntityId == model.IndividualId &&
            (db.Jobs.Any(j => j.IndividualID == x.BusinessEntityId && j.OrganisationID == org) || db.Users.Any(u => u.BusinessEntityID == x.BusinessEntityId && u.UserOrganisation!.BusinessEntityID == org))))
            return ServiceResult.Fail("Select an individual associated with this organisation.");
        var entity = await db.Staffs.SingleOrDefaultAsync(x => x.IndividualId == model.IndividualId);
        if (entity != null) await RequireAsync("Staff.Admin.Edit");
        else await RequireAsync("Staff.Add.Admin", "Staff.Add");
        if (!string.IsNullOrWhiteSpace(model.EmployeeNumber) && await db.Staffs.AnyAsync(x => x.IndividualId != model.IndividualId && x.EmployeeNumber == model.EmployeeNumber.Trim()))
            return ServiceResult.Fail("This employee number is already in use.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.STAFF_CREATE, $"Saved staff record {model.IndividualId}");
        if (entity == null) { entity = new Staff { IndividualId = model.IndividualId }; db.Staffs.Add(entity); }
        entity.EmployeeNumber = model.EmployeeNumber?.Trim(); entity.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return ServiceResult.Ok("Staff record saved.");
    }

    public async Task<ServiceResult> SaveEnrollmentAsync(ManagedEnrollmentDto model)
    {
        var (_, org) = await RequireAsync("Staff.EnrolmentNumbers.Manage");
        if (string.IsNullOrWhiteSpace(model.Number) || model.Number.Trim().Length > 10) return ServiceResult.Fail("Enter an enrollment number of up to 10 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Staffs.AnyAsync(x => x.IndividualId == model.IndividualId && (x.Jobs.Any(j => j.OrganisationID == org) ||
            db.Users.Any(u => u.BusinessEntityID == x.IndividualId && u.UserOrganisation!.BusinessEntityID == org)))) return ServiceResult.Fail("Select staff from this organisation.");
        if (await db.StaffEnrollmentNumbers.AnyAsync(x => x.OrganisationId == org && x.StaffEnrollmentNumberId != model.Id && x.EnrollmentNumber == model.Number.Trim()))
            return ServiceResult.Fail("This enrollment number is already in use in this organisation.");
        var entity = model.Id == 0 ? new StaffEnrollmentNumber { OrganisationId = org } : await db.StaffEnrollmentNumbers.SingleOrDefaultAsync(x => x.StaffEnrollmentNumberId == model.Id && x.OrganisationId == org);
        if (entity == null) return ServiceResult.Fail("Enrollment record was not found.");
        if (model.Id != 0 && entity.IndividualId != model.IndividualId) return ServiceResult.Fail("An existing enrollment cannot be reassigned to another staff member.");
        var log = await logs.CreateAsync(db, model.Id == 0 ? SharedConfig.OperationLogActionTypes.STAFF_ENROLLMENT_NUMBER_CREATE : SharedConfig.OperationLogActionTypes.STAFF_ENROLLMENT_NUMBER_UPDATE, $"Saved staff enrollment for {model.IndividualId}");
        if (model.Id == 0) db.StaffEnrollmentNumbers.Add(entity);
        entity.IndividualId = model.IndividualId; entity.EnrollmentNumber = model.Number.Trim(); entity.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.StaffEnrollmentNumberId; return ServiceResult.Ok("Enrollment number saved.");
    }

    public async Task<ServiceResult> SaveOrganisationAccessAsync(List<int> permissionIds)
    {
        var service = new OrganisationPermissionService(factory, access, logs, contexts);
        var data = await service.LoadAsync();
        return await service.SaveAsync(data.OrganisationId, permissionIds, data.AllowedIds, "Updated organisation permission limits.");
    }
}
