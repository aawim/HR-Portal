using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class OrganisationPermissionTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var actor = 1;
        var effective = new EffectivePermissionService(factory);
        var invalidated = new HashSet<int>();
        async Task<UserContextDto> Context() => new() { UserId = actor, IndividualId = actor * 100,
            Permissions = await effective.GetEffectivePermissionsAsync(actor) };
        var access = Proxy.For<IUserAccessService>((m, a) => m switch {
            "RequireContextAsync" => Context(), "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == 10),
            "HasPermissionAsync" => effective.HasPermissionAsync(actor, (string)a[0]!), _ => throw new Exception(m) });
        var logs = Proxy.For<IOperationLogService>((m, a) => m == "CreateAsync" ? Log((HrmTeContext)a[0]!) : throw new Exception(m));
        var contexts = Proxy.For<IUserContextService>((m, a) => { if (m != "InvalidateAsync") throw new Exception(m); invalidated.Add((int)a[0]!); return Task.CompletedTask; });
        var service = new OrganisationPermissionService(factory, access, logs, contexts);
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "First" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Other" });
            db.UserOrganisations.AddRange(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 }, new UserOrganisation { UserOrganisationID = 2, BusinessEntityID = 20 });
            for (var id = 1; id <= 2; id++) db.Users.Add(new User { UserId = id, BusinessEntityID = id * 100, Username = "user" + id, UserOrganisationId = 1,
                IsActive = true, LocalUserStateId = 1, AdminPortalAccessEnabled = true, AdminPortalAccessOrganisationId = 1 });
            db.Roles.AddRange(new Role { RoleID = 1, RoleKey = "Admin", Name = "Admin", ContextId = 1 },
                new Role { RoleID = 2, RoleKey = "Users.View", Name = "View", ContextId = 1 },
                new Role { RoleID = 3, RoleKey = "Super.Admin.Manage", Name = "Platform management", ContextId = 1 },
                new Role { RoleID = 4, RoleKey = "Restricted", Name = "Restricted", ContextId = 1 });
            db.UserOrganisationRoles.AddRange(new UserOrganisationRole { UserOrganisationID = 1, RoleID = 1, IsActive = true }, new UserOrganisationRole { UserOrganisationID = 1, RoleID = 2, IsActive = true });
            db.UserRoles.AddRange(new() { UserId = 1, UserOrganisationId = 1, RoleId = 3, IsActive = true },
                new() { UserId = 2, UserOrganisationId = 1, RoleId = 2, IsActive = true }, new() { UserId = 2, UserOrganisationId = 1, RoleId = 4, IsActive = true });
            db.UserGroups.Add(new UserGroup { UserGroupId = 1, UserOrganisationId = 1, GroupName = "Group" });
            db.UserGroupRoles.AddRange(new UserGroupRole { UserGroupId = 1, RoleId = 2, IsActive = true }, new UserGroupRole { UserGroupId = 1, RoleId = 4, IsActive = true });
            db.UserAssignedUserGroups.Add(new UserAssignedUserGroup { UserId = 2, UserGroupId = 1 });
            await db.SaveChangesAsync();
        }
        Check(!(await effective.GetEffectivePermissionsAsync(2)).Any(p => p.RoleID == 4), "Direct/group permission escaped organisation ceiling");
        Check((await effective.LoadDirectPermissionsAsync(2)).Select(p => p.RoleID).SequenceEqual([2]), "Direct permission source bypassed ceiling");
        Check((await effective.LoadUserGroupPermissionsAsync(2)).Select(p => p.RoleID).SequenceEqual([2]), "Group permission source bypassed ceiling");
        var data = await service.LoadAsync();
        Check(data.IsSuperAdmin && data.Organisations.Count == 2 && data.AllowedIds.Count == 2, "Explicit platform administrator cannot manage permission limits");
        Check((await service.LoadAsync(20)).OrganisationId == 20, "Super Admin organisation selection failed");
        actor = 2;
        var denied = false; try { await service.LoadAsync(20); } catch (UnauthorizedAccessException) { denied = true; }
        Check(denied, "Organisation administrator selected another tenant");
        Check(!(await service.SaveAsync(10, [1,2,4], [1,2], "Escalate")).Success, "Tenant administrator expanded their own ceiling");
        actor = 1;
        Check(!(await service.SaveAsync(10, [1,4], [1], "Stale")).Success, "Stale organisation settings overwrote current permissions");
        Check(!(await service.SaveAsync(10, [1,4], [1,2], "")).Success, "Unaudited change accepted");
        Check((await service.SaveAsync(10, [1,4], [1,2], "Enable restricted area; remove view")).Success, "Super Admin save failed");
        Check(await effective.HasPermissionAsync(2, "Restricted") && !await effective.HasPermissionAsync(2, "Users.View"), "Permission change did not apply immediately");
        Check(invalidated.SetEquals([1,2]), "Tenant users were not invalidated");
        await using (var db = factory.CreateDbContext())
        {
            Check(await db.UserRoles.AnyAsync(r => r.UserId == 2 && r.RoleId == 2 && r.IsActive), "Limit removal deleted the underlying user assignment");
            Check(await db.OperationLogs.AnyAsync(), "Organisation permission audit missing");
        }
        Check((await service.SaveAsync(10, [], [1,4], "Disable organisation resources")).Success, "Super Admin cannot clear tenant permissions");
        Check(!(await effective.GetEffectivePermissionsAsync(2)).Any(), "Empty ceiling did not deny user/group permissions");
        Check((await service.LoadAsync()).IsSuperAdmin, "Explicit platform administrator lost recovery access after clearing tenant resources");
    }
    private static async Task<OperationLog> Log(HrmTeContext db)
    {
        var row = new OperationLog { CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, CreatedByIp = "", UpdatedByIp = "" };
        db.OperationLogs.Add(row); await db.SaveChangesAsync(); return row;
    }
}
