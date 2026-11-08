using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class TeamManagementTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public async Task Hr_group_membership_without_effective_write_permission_cannot_create_teams(int organisation)
    {
        var factory = new QueryFactory(); var access = new FakeAccess(); access.Actor!.IsHRStaff = true;
        var logs = StubProxy.For<IOperationLogService>((_, _) => throw new Exception("Unauthorised writes must not reach auditing."));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new TeamService(factory, access, logs).CreateAsync(organisation, "Team", null, new(2026, 10, 28)));
        await using var db = factory.CreateDbContext(); Assert.Empty(await db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Canonical_team_creation_records_audit_and_rejects_duplicate_names_in_the_same_organisation()
    {
        var factory = new QueryFactory(ignoreTransactions: true); var access = new FakeAccess(); access.Permissions.Add("Organisation.Edit");
        await using (var db = factory.CreateDbContext()) { db.Organisations.Add(new() { BusinessEntityID = 10, OrganisationName = "Tenant" }); await db.SaveChangesAsync(); }
        var calls = 0;
        var logs = StubProxy.For<IOperationLogService>((name, _) => { Assert.Equal("CreateAsync", name); calls++; return Task.FromResult(new OperationLog { OperationLogId = 99 }); });
        var service = new TeamService(factory, access, logs);
        var id = await service.CreateAsync(10, "  Support  ", null, new(2026, 10, 28));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(10, "Support", null, new(2026, 10, 28)));
        await using var verify = factory.CreateDbContext(); var team = Assert.Single(await verify.Teams.ToListAsync());
        Assert.Equal(id, team.TeamId); Assert.Equal("Support", team.Name); Assert.Equal(10, team.OrganisationId);
        Assert.Equal(99, team.OperationLogId); Assert.Equal(1, calls);
    }
}
