using HRM.Services.Communications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CommunicationAccessTests
{
    private static CommunicationHubService Service(FakeAccess access, QueryFactory factory) => new(factory, access,
        null!, new ConfigurationBuilder().Build(), NullLogger<CommunicationHubService>.Instance,
        null!, null!, null!, null!);

    [Theory]
    [InlineData(true, false, false, true, 10, true)]
    [InlineData(false, true, false, true, 10, true)]
    [InlineData(false, false, true, true, 10, true)]
    [InlineData(true, false, false, false, 10, false)]
    [InlineData(false, true, false, false, 10, false)]
    [InlineData(false, false, true, false, 10, false)]
    [InlineData(false, false, false, true, 10, false)]
    [InlineData(true, false, false, true, 20, false)]
    public async Task AccessRequiresTenantPortalAdmissionAndAuthority(bool organisationAdministrator, bool adminPermission, bool communicationPermission, bool portal, int accountOrganisation, bool expected)
    {
        var factory = new QueryFactory();
        var access = new FakeAccess();
        access.Actor!.IsOrganisationAdmin = organisationAdministrator;
        if (adminPermission) access.Permissions.Add("Admin");
        if (communicationPermission) access.Permissions.Add("Communication.Manage");
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new() { UserId = 1, IsActive = true, LocalUserStateId = 1,
                AdminPortalAccessEnabled = portal, AdminPortalAccessOrganisationId = 1,
                UserOrganisationId = 1, UserOrganisation = new() { UserOrganisationID = 1, BusinessEntityID = accountOrganisation } });
            await db.SaveChangesAsync();
        }
        if (expected) Assert.NotNull(await Service(access, factory).LoadAsync());
        else await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(access, factory).LoadAsync());
    }

    [Fact]
    public async Task OrdinaryStaffCannotLoadCommunicationSettings()
    {
        var access = new FakeAccess();
        access.Actor!.IsHRStaff = true;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(access, new()).LoadAsync());
    }

    [Fact]
    public async Task SuperAdministratorStillRequiresAnOrganisationContext()
    {
        var access = new FakeAccess { Organisation = 0 };
        access.Actor!.IsSuperAdministrator = true;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(access, new()).LoadAsync());
    }

    [Fact]
    public async Task PlatformAuthorityAloneDoesNotGrantTenantCommunicationAccess()
    {
        var factory = new QueryFactory(); var access = new FakeAccess();
        access.Actor!.IsSuperAdministrator = true;
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new() { UserId = 1, IsActive = true, LocalUserStateId = 1,
                AdminPortalAccessEnabled = true, AdminPortalAccessOrganisationId = 1,
                UserOrganisationId = 1, UserOrganisation = new() { UserOrganisationID = 1, BusinessEntityID = 10 } });
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(access, factory).LoadAsync());
    }
}
