using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class LeaveFlowConfigurationTests
{
    [Fact]
    public async Task MissingTransitionsSaveTheFlowAndCreateSeparateResolutionAlerts()
    {
        var factory = await SeedAsync();
        var service = Service(factory);
        var model = Model();

        var result = await service.SaveAsync(model);
        Assert.True(result.Success, result.Message);
        Assert.Contains("Approval flow saved", result.Message);
        Assert.Contains("must be configured by a Developer", result.Message);
        Assert.Equal(2, model.Version);
        var retry = await service.SaveAsync(model);
        Assert.True(retry.Success, retry.Message);

        await using var db = factory.CreateDbContext();
        var alerts = await db.StaffNotifications.OrderBy(x => x.Url).ToListAsync();
        Assert.Equal(3, alerts.Count);
        foreach (var (from, to) in new[] { (1, 2), (2, 3), (3, 4) })
        {
            var alert = Assert.Single(alerts, x => x.Url ==
                $"/developer/notification-resolution?jobTypeId=7&requestTypeId=118&fromStateId={from}&toStateId={to}");
            Assert.Equal("Request transition needs attention", alert.Kind);
            Assert.Equal(1, alert.UserId);
            Assert.Equal(10, alert.OrganisationId);
            Assert.Contains("Supervisor Assignment Request", alert.Message);
            Assert.Contains("Missing transition", alert.Message);
        }
        Assert.Empty(await db.RequestTypesAllowedRequestStateTransitions.ToListAsync());
        var definitions = await db.WfWorkflowDefinitions.Include(x => x.Stages).ToListAsync();
        Assert.Equal(3, definitions.Count);
        var active = Assert.Single(definitions, x => x.IsActive);
        Assert.Equal(3, active.VersionNumber);
        Assert.Equal(new[] { 2, 3 }, active.Stages.OrderBy(x => x.StageOrder).Select(x => x.RequestStateId));
        Assert.Equal(3, model.Version);
    }

    [Fact]
    public async Task ConfiguredTransitionsSaveTheFlowWithoutAlerts()
    {
        var factory = await SeedAsync();
        await using (var db = factory.CreateDbContext())
        {
            foreach (var (from, to) in new[] { (1, 2), (2, 3), (3, 4) })
                db.RequestTypesAllowedRequestStateTransitions.Add(new RequestTypesAllowedRequestStateTransition
                {
                    RequestTypeId = 118, FromRequestStateId = from, ToRequestStateId = to
                });
            await db.SaveChangesAsync();
        }

        var model = Model();
        Assert.True((await Service(factory).SaveAsync(model)).Success);

        await using var check = factory.CreateDbContext();
        Assert.Empty(await check.StaffNotifications.ToListAsync());
        Assert.Equal(2, model.Version);
        Assert.Equal(2, (await check.WfWorkflowDefinitions.SingleAsync(x => x.IsActive)).VersionNumber);
    }

    private static LeaveFlowConfigurationService Service(QueryFactory factory)
    {
        var access = new FakeAccess();
        access.Actor!.IsHRStaff = true;
        return new(factory, access);
    }

    private static LeaveFlowConfigurationDto Model() => new()
    {
        JobTypeId = 7, RequestTypeId = 118, Version = 1,
        Stages =
        [
            new() { Name = "Supervisor verification", StateId = 2, UserGroupId = 8, ApplicantSupervisorOnly = true },
            new() { Name = "HR approval", StateId = 3, UserGroupId = 8 }
        ]
    };

    private static async Task<QueryFactory> SeedAsync()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using var db = factory.CreateDbContext();
        db.Organisations.Add(new() { BusinessEntityID = 10, OrganisationName = "Test organisation" });
        db.JobTypes.Add(new() { JobTypeId = 7, TypeName = "Permanent", IsActive = true });
        db.RequestTypes.Add(new() { RequestTypeId = 118, TypeName = "Supervisor Assignment Request" });
        var names = new[] { "Incomplete", "Pending Verification", "Pending Approval", "Approved", "Cancelled", "Rejected" };
        for (var id = 1; id <= names.Length; id++)
            db.RequestStates.Add(new() { RequestStateId = id, StateName = names[id - 1], IsProcessingState = id <= 3, IsFinalState = id >= 4 });
        db.UserGroups.Add(new() { UserGroupId = 8, GroupName = "Developer", IsGlobal = true,
            UserOrganisationId = 1, UserOrganisation = new() { UserOrganisationID = 1, BusinessEntityID = 10 } });
        db.Users.Add(new() { UserId = 1, BusinessEntityID = 100, IsActive = true, LocalUserStateId = 1,
            UserOrganisationId = 1, UserAssignedUserGroups = [new() { UserGroupId = 8 }] });
        db.WfWorkflowDefinitions.Add(new WfWorkflowDefinition
        {
            OrganisationBusinessEntityId = 10, RequestTypeId = 118, LeaveRequestKind = 0,
            Name = "Existing flow", VersionNumber = 1, WorkflowJobTypes = [new() { JobTypeId = 7 }]
        });
        await db.SaveChangesAsync();
        return factory;
    }
}
