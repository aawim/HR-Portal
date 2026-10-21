using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class AccessGroupTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var factory = new ReceiptFactory(options);
        var user = new UserContextDto { UserId = 425, IndividualId = 26,
            Organisation = new UserOrganisationDto { OrganisationBusinessEntityId = 10 },
            Permissions = [new() { RoleID = 2, RoleKey = "Developer" }] };
        var invalidated = new List<int>();
        var contexts = Proxy.For<IUserContextService>((name, args) => name switch
        {
            "GetCurrentAsync" => Task.FromResult<UserContextDto?>(user),
            "InvalidateAsync" => Invalidate((int)args[0]!),
            _ => throw new Exception(name)
        });
        Task Invalidate(int id) { invalidated.Add(id); return Task.CompletedTask; }
        var access = new UserAccessService(contexts, null!, factory);
        await using var auditContext = factory.CreateDbContext();
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new User { UserId = 425, BusinessEntityID = 26, UserOrganisationId = 1, IsActive = true, LocalUserStateId = 1, Username = "Staff" });
            db.UserOrganisations.Add(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 });
            db.Roles.AddRange(new Role { RoleID = 2, RoleKey = "Developer", Name = "Developer" }, new Role { RoleID = 1, RoleKey = "Admin", Name = "Admin" });
            await db.SaveChangesAsync();
        }
        var scopedAccess = Proxy.For<IUserAccessService>((name, args) => name switch
        {
            "RequireContextAsync" => Task.FromResult(user),
            "HasPermissionAsync" => Task.FromResult((string)args[0]! == "Developer"),
            "HasOrganisationAccessAsync" => Task.FromResult((int)args[0]! == 10),
            _ => throw new Exception(name)
        });
        var logs = new OperationLogService(auditContext, null!, null!, new HttpContextAccessor(), null!, scopedAccess);
        var settings = new PermissionSettingsService(factory, scopedAccess, logs, contexts);
        var group = new AccessRoleDto { Name = "Developer", PermissionIds = [2] };
        var saved = await settings.SaveRoleAsync(group);
        Check(saved.Success && group.Id > 0, "Developer permission holder cannot create a User Group");
        await using (var db = factory.CreateDbContext())
        {
            Check((await db.UserGroups.SingleAsync()).UserOrganisationId == 1 && (await db.UserGroupRoles.SingleAsync()).RoleId == 2,
                "Group or its assigned permission was not saved");
            var audit = await db.OperationLogs.SingleAsync();
            Check(audit.CreatedByUserOrganisationId == 1 && audit.UpdatedByUserOrganisationId == 1,
                "Audit used organisation business ID instead of UserOrganisation ID");
        }
        Check(!await access.HasUserGroupAsync("Developer"), "Direct Developer permission incorrectly counted as group membership");
        await using (var db = factory.CreateDbContext())
        {
            db.UserAssignedUserGroups.Add(new UserAssignedUserGroup { UserId = 425, UserGroupId = group.Id });
            await db.SaveChangesAsync();
        }
        Check(await access.HasUserGroupAsync("developer"), "New membership was hidden by a stale cached context");
        Check((await settings.LoadAsync(true)).Roles.Any(x => x.Id == group.Id), "Created group missing from roles settings");
        Check((await settings.SaveRoleAsync(group)).Success && invalidated.Contains(425), "Saving grouped permissions did not invalidate member context");
        group.PermissionIds.Add(1);
        Check(!(await settings.SaveRoleAsync(group)).Success, "Developer could grant an unrelated Admin permission they do not hold");
        await using (var db = factory.CreateDbContext())
        {
            db.UserAssignedUserGroups.Remove(await db.UserAssignedUserGroups.SingleAsync());
            await db.SaveChangesAsync();
        }
        Check(!await access.HasUserGroupAsync("Developer"), "Removed membership remained authorised through cache");
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
