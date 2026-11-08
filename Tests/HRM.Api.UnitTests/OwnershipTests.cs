using HRM.Application.Contracts.Pages;
using HRM.Models;
using HRM.Services;
using HRM.Services.Queries;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class OwnershipTests
{
    [Fact]
    public async Task Leave_workflow_queries_only_return_the_current_employees_leaves()
    {
        var access = new FakeAccess(); var factory = new QueryFactory();
        await using (var db = factory.CreateDbContext())
        {
            db.RequestStates.Add(new() { RequestStateId = 7, StateName = "Pending HR Approval", IsProcessingState = true });
            db.Jobs.AddRange(new() { JobId = 1, IndividualID = 100, OrganisationID = 10 }, new() { JobId = 2, IndividualID = 200, OrganisationID = 10 });
            db.Leaves.AddRange(new() { LeaveId = 11, JobId = 1 }, new() { LeaveId = 22, JobId = 2 });
            db.Requests.AddRange(new() { RequestId = 1, RequestStateId = 7, ReferenceNumber = "LV-1" }, new() { RequestId = 2, RequestStateId = 7, ReferenceNumber = "LV-2" });
            db.LeaveRequests.AddRange(new() { RequestId = 1, LeaveId = 11 }, new() { RequestId = 2, LeaveId = 22 });
            await db.SaveChangesAsync();
        }
        var service = new LeaveRequestWorkflowQueries(factory, access);
        var row = Assert.Single(await service.GetMineAsync([11, 22]));
        Assert.Equal(11, row.LeaveId); Assert.True(row.IsProcessing); Assert.False(row.IsFinal); Assert.Equal("LV-1", row.Reference);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetMineAsync([-1]));
    }
    [Fact]
    public async Task Leave_filter_states_do_not_reuse_workflow_request_state_ids()
    {
        var factory = new QueryFactory(); var access = new FakeAccess();
        await using (var db = factory.CreateDbContext())
        {
            db.LeaveStates.Add(new() { LeaveStateId = 5, StateName = "Rejected" });
            db.RequestStates.Add(new() { RequestStateId = 6, StateName = "Rejected", IsFinalState = true });
            await db.SaveChangesAsync();
        }
        var service = new LeaveRequestWorkflowQueries(factory, access);
        Assert.Equal(5, Assert.Single(await service.GetLeaveStatesAsync()).Id);
        Assert.Equal(6, Assert.Single(await service.GetStatesAsync()).Id);
    }
    [Fact]
    public async Task User_profile_lookup_cannot_read_another_tenants_staff()
    {
        var factory = new QueryFactory(); var access = new FakeAccess();
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new() { UserId = 2, BusinessEntityID = 200, Username = "foreign" });
            db.Jobs.Add(new() { JobId = 2, IndividualID = 200, OrganisationID = 20 });
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new UserService(factory, access).GetByIdAsync(2));
    }
    [Fact]
    public async Task Organisation_panels_deny_foreign_units_even_when_their_ids_are_known()
    {
        var factory = new QueryFactory(); var access = new FakeAccess(); access.Permissions.Add("Employee.View");
        await using (var db = factory.CreateDbContext()) { db.Teams.Add(new() { TeamId = 20, OrganisationId = 20, Name = "Foreign", IsValid = true }); await db.SaveChangesAsync(); }
        var service = new OrganisationDirectoryQueries(factory, access, new OrganisationService(factory, access), new(access));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDetailsAsync(new(OrganisationUnitKind.Team, 20)));
        Assert.Empty(await service.GetTeamsAsync());
    }
}
