using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class PlatformContextTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task Context_platform_authority_requires_an_explicit_grant_and_portal_admission(bool grant, bool portal, bool expected)
    {
        var factory = new QueryFactory();
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.Add(new() { BusinessEntityID = 10, OrganisationName = "Tenant" });
            db.UserOrganisations.Add(new() { UserOrganisationID = 1, BusinessEntityID = 10 });
            db.Users.Add(new() { UserId = 1, Username = "staff", BusinessEntityID = 100, IsActive = true, LocalUserStateId = 1,
                UserOrganisationId = 1, AdminPortalAccessEnabled = portal, AdminPortalAccessOrganisationId = portal ? 1 : null });
            db.Individuals.Add(new() { BusinessEntityId = 100, FirstNameEnglish = "Staff" });
            db.Roles.Add(new() { RoleID = 1, RoleKey = "Super.Admin.Manage", Name = "Platform management", ContextId = 1 });
            db.UserGroups.Add(new() { UserGroupId = 1, GroupName = "Super Administrator", UserOrganisationId = 1 });
            db.UserAssignedUserGroups.Add(new() { UserId = 1, UserGroupId = 1 });
            if (grant) db.UserRoles.Add(new() { UserId = 1, UserOrganisationId = 1, RoleId = 1, IsActive = true });
            await db.SaveChangesAsync();
        }
        var roles = StubProxy.For<IRoleService>((_, _) => throw new Exception("Do not use unscoped permissions."));
        var builder = new UserContextBuilder(factory, null!, roles, new EffectivePermissionService(factory));
        var context = await builder.BuildAsync(1);
        Assert.NotNull(context); Assert.Equal(expected, context.IsSuperAdministrator);
        // A group's name by itself is never a platform grant.
        Assert.Single(context.UserGroups);
    }
}
