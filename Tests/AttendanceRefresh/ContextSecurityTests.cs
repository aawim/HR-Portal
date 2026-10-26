using System.Security.Claims;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

static class ContextSecurityTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options;
        var factory = new ReceiptFactory(options);
        var auth = new ContextAuthentication();
        auth.SignIn("one");
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "First" },
                new Organisation { BusinessEntityID = 20, OrganisationName = "Other" });
            db.UserOrganisations.AddRange(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 },
                new UserOrganisation { UserOrganisationID = 2, BusinessEntityID = 20 });
            db.Users.AddRange(new User { UserId = 1, Username = "one", BusinessEntityID = 100, UserOrganisationId = 1, IsActive = true, LocalUserStateId = 1 },
                new User { UserId = 2, Username = "two", BusinessEntityID = 200, UserOrganisationId = 2, IsActive = true, LocalUserStateId = 1 });
            db.Roles.Add(new Role { RoleID = 1, RoleKey = "Developer", Name = "Developer", ContextId = 1 });
            db.UserGroups.AddRange(new UserGroup { UserGroupId = 1, GroupName = "Developer", UserOrganisationId = 1 },
                new UserGroup { UserGroupId = 2, GroupName = "Foreign", UserOrganisationId = 2 });
            db.UserGroupRoles.AddRange(new UserGroupRole { UserGroupId = 1, RoleId = 1, IsActive = true },
                new UserGroupRole { UserGroupId = 2, RoleId = 1, IsActive = true });
            // An incorrectly assigned foreign group must never grant permissions.
            db.UserAssignedUserGroups.Add(new UserAssignedUserGroup { UserId = 1, UserGroupId = 2 });
            await db.SaveChangesAsync();
        }
        var sessionService = new HRM.Services.Interfaces.UserContext(auth, factory);
        var session = await sessionService.GetSessionAsync();
        Check(session?.OrganisationId == 10 && !session.CanAccessAdminPortal, "Foreign group or forged org claim granted Admin access");
        var organisationService = Proxy.For<IOrganisationService>((method, args) => method == "GetUserOrganisationAsync"
            ? Task.FromResult<UserOrganisationDto?>(new UserOrganisationDto { OrganisationBusinessEntityId = 10 })
            : throw new Exception("Organisation must be loaded through the user's assignment: " + method));
        var roleService = Proxy.For<IRoleService>((method, args) => throw new Exception("Unscoped role service must not override resolved permissions"));
        var realBuilder = new UserContextBuilder(factory, auth, roleService, organisationService, new EffectivePermissionService(factory));
        var built = await realBuilder.BuildAsync(1);
        Check(built?.Organisation?.OrganisationBusinessEntityId == 10 && built.UserGroups.Count == 0 && built.Permissions.Count == 0,
            "Builder used an individual ID for organisation lookup or retained foreign groups");
        await using (var db = factory.CreateDbContext())
        {
            db.UserAssignedUserGroups.Add(new UserAssignedUserGroup { UserId = 1, UserGroupId = 1 });
            await db.SaveChangesAsync();
        }
        Check((await sessionService.GetSessionAsync())?.CanAccessAdminPortal == false, "A User Group granted portal access without explicit approval");
        await using (var db = factory.CreateDbContext())
        {
            db.UserOrganisationRoles.Add(new UserOrganisationRole { UserOrganisationID = 1, RoleID = 1, IsActive = true });
            await db.SaveChangesAsync();
        }
        Check(!(await new EffectivePermissionService(factory).GetEffectivePermissionsAsync(1)).Any(),
            "Organisation defaults bypassed the explicit Admin portal grant");
        await using (var db = factory.CreateDbContext())
        {
            db.UserOrganisationRoles.RemoveRange(db.UserOrganisationRoles);
            await db.SaveChangesAsync();
        }
        await using (var db = factory.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(x => x.UserId == 1);
            user.AdminPortalAccessEnabled = true;
            user.AdminPortalAccessOrganisationId = 1;
            await db.SaveChangesAsync();
        }
        var portalOnlySession = await sessionService.GetSessionAsync();
        Check(portalOnlySession?.CanAccessAdminPortal == true, $"Explicit portal admission incorrectly required resource permissions (user={portalOnlySession?.UserId}, org={portalOnlySession?.OrganisationId})");
        Check(!(await new EffectivePermissionService(factory).GetEffectivePermissionsAsync(1)).Any(), "Portal admission bypassed the organisation resource ceiling");
        await using (var db = factory.CreateDbContext())
        {
            db.UserOrganisationRoles.Add(new UserOrganisationRole { UserOrganisationID = 1, RoleID = 1, IsActive = true });
            await db.SaveChangesAsync();
        }
        Check((await sessionService.GetSessionAsync())?.CanAccessAdminPortal == true, "Explicitly granted Admin access was denied");
        await using (var db = factory.CreateDbContext())
        {
            (await db.Users.SingleAsync(x => x.UserId == 1)).AdminPortalAccessEnabled = false;
            await db.SaveChangesAsync();
        }
        Check((await sessionService.GetSessionAsync())?.CanAccessAdminPortal == false &&
            !(await new EffectivePermissionService(factory).GetEffectivePermissionsAsync(1)).Any(),
            "Revoked portal grant retained Admin permissions");
        await using (var db = factory.CreateDbContext())
        {
            (await db.Users.SingleAsync(x => x.UserId == 1)).AdminPortalAccessEnabled = true;
            await db.SaveChangesAsync();
        }
        await using (var db = factory.CreateDbContext())
        {
            db.UserAssignedUserGroups.Remove(await db.UserAssignedUserGroups.SingleAsync(x => x.UserId == 1 && x.UserGroupId == 1));
            db.UserOrganisationRoles.RemoveRange(db.UserOrganisationRoles);
            await db.SaveChangesAsync();
        }
        Check((await sessionService.GetSessionAsync())?.CanAccessAdminPortal == true, "Removing resource assignments also removed explicit portal admission");
        Check(!(await new EffectivePermissionService(factory).GetEffectivePermissionsAsync(1)).Any(), "Removed User Group permissions survived refresh");
        await using (var db = factory.CreateDbContext())
        {
            (await db.Users.SingleAsync(x => x.UserId == 1)).UserOrganisationId = 2;
            await db.SaveChangesAsync();
        }
        session = await sessionService.GetSessionAsync();
        Check(session?.OrganisationId == 20 && !session.CanAccessAdminPortal, "Admin portal grant crossed organisations");

        var builds = 0;
        var builder = Proxy.For<IUserContextBuilder>((method, args) =>
        {
            if (method != "BuildAsync") throw new Exception(method);
            builds++;
            return Task.FromResult<UserContextDto?>(new UserContextDto { UserId = (int)args[0]! });
        });
        var cache = Proxy.For<IUserContextCache>((method, args) => method switch
        {
            "SetAsync" or "RemoveAsync" => Task.CompletedTask,
            _ => throw new Exception("Security must not depend on a cached context: " + method)
        });
        var contexts = new UserContextService(builder, cache, factory, auth);
        Check((await contexts.GetCurrentAsync())?.UserId == 1, "Wrong current identity");
        await contexts.GetCurrentAsync();
        Check(builds == 2, "Authorization context was not refreshed");
        var denied = false;
        try { await contexts.GetAsync(2); } catch (UnauthorizedAccessException) { denied = true; }
        Check(denied, "User could impersonate another account through GetAsync");
        auth.SignIn("two");
        Check((await contexts.GetCurrentAsync())?.UserId == 2, "Prior user's context survived identity change");
        await using (var db = factory.CreateDbContext())
        {
            (await db.Users.SingleAsync(x => x.UserId == 2)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Check(await contexts.GetCurrentAsync() == null && await sessionService.GetSessionAsync() == null, "Disabled account retained access");
        auth.SignIn("unregistered-efaas-user");
        Check(await sessionService.GetSessionAsync() == null,
            "External authentication granted system access without a local HRM account");
        auth.SignOut();
        Check(await contexts.GetCurrentAsync() == null && contexts.Current == null && await sessionService.GetSessionAsync() == null,
            "Sign out retained a user context");

        var portal = new PortalContext();
        portal.SwitchToAdmin();
        Check(!portal.IsAdminPortal, "Uninitialised portal allowed Admin");
        portal.Initialize(10, "First", true);
        portal.SwitchToAdmin();
        Check(portal.IsAdminPortal && portal.CurrentContext == HRM.Enum.TransactionContext.Admin, "Admin audit context incorrect");
        portal.SwitchToEmployee();
        Check(!portal.IsAdminPortal && portal.CurrentContext == HRM.Enum.TransactionContext.Online, "Self audit context incorrect");
        portal.SwitchToAdmin();
        portal.Initialize(null, "", false);
        Check(!portal.IsAdminPortal && portal.CurrentOrganisationId == null, "Revocation retained Admin context");
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}

sealed class ContextAuthentication : AuthenticationStateProvider
{
    ClaimsPrincipal principal = new(new ClaimsIdentity());
    public void SignIn(string username) => principal = new(new ClaimsIdentity([
        new Claim("idnumber", username), new Claim("orgId", "999"), new Claim("orgName", "Forged")], "Test"));
    public void SignOut() => principal = new(new ClaimsIdentity());
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal));
}
