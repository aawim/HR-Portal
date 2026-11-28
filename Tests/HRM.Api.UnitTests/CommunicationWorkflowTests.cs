using HRM.Communications.Contracts;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.Communications;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Communications;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CommunicationWorkflowTests
{
    private sealed class Fixture
    {
        public QueryFactory Factory { get; } = new(ignoreTransactions: true);
        public FakeAccess Access { get; } = new();
        public CommunicationHubService Service { get; }
        public Fixture()
        {
            Service = new(Factory, Access,
                StubProxy.For<ICommunicationConnectionValidator>((_, _) => throw new InvalidOperationException("No provider calls are permitted.")),
                new ConfigurationBuilder().Build(), NullLogger<CommunicationHubService>.Instance,
                StubProxy.For<IWorkflowEngine>((_, _) => throw new InvalidOperationException("Workflow already submitted.")),
                StubProxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = 1 })),
                StubProxy.For<IWorkflowApproverResolver>((_, args) => Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>([new((int)args[0]!, 1, 1, true)])),
                StubProxy.For<IWorkflowRequestContextResolver>((_, _) => Task.FromResult(new WorkflowRequestContextDto())));
        }

        public async Task Seed(bool twoStages = false)
        {
            await using var db = Factory.CreateDbContext();
            var organisation = new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 };
            db.Users.AddRange(new() { UserId = 1, BusinessEntityID = 100, Username = "reviewer", IsActive = true, LocalUserStateId = 1, UserOrganisation = organisation },
                new() { UserId = 2, BusinessEntityID = 300, Username = "final", IsActive = true, LocalUserStateId = 1, UserOrganisation = organisation },
                new() { UserId = 3, BusinessEntityID = 200, Username = "submitter", IsActive = true, LocalUserStateId = 1, UserOrganisation = organisation });
            db.RequestStates.AddRange(new() { RequestStateId = 2, StateName = "Review", IsProcessingState = true },
                new() { RequestStateId = 7, StateName = "Final review", IsProcessingState = true }, new() { RequestStateId = 4, StateName = "Approved", IsFinalState = true },
                new() { RequestStateId = 5, StateName = "Cancelled", IsFinalState = true }, new() { RequestStateId = 6, StateName = "Rejected", IsFinalState = true });
            db.RequestTypes.Add(new() { RequestTypeId = 900, TypeName = "Communication Announcement" });
            db.Requests.Add(new() { RequestId = 1, RequestTypeId = 900, RequestStateId = 2, ApplicantBusinessEntityId = 200, OrganisationBusinessEntityID = 10 });
            db.Jobs.Add(new() { JobId = 10, IndividualID = 200, OrganisationID = 10, JobStateId = 4 });
            var definition = new WfWorkflowDefinition { WorkflowDefinitionId = 1, RequestTypeId = 900, Name = "Announcements" };
            var first = new WfWorkflowStage { WorkflowStageId = 1, WorkflowDefinition = definition, StageOrder = 1, StageName = "Review", RequestStateId = 2 };
            definition.Stages.Add(first);
            if (twoStages) definition.Stages.Add(new() { WorkflowStageId = 2, WorkflowDefinition = definition, StageOrder = 2, StageName = "Final review", RequestStateId = 7 });
            var instance = new WfRequestInstance { WorkflowRequestInstanceId = 1, RequestId = 1, WorkflowDefinition = definition };
            var stage = new WfRequestStageInstance { RequestStageInstanceId = 1, WorkflowRequestInstance = instance, WorkflowStage = first };
            stage.Approvals.Add(new() { ApproverUserId = 1 }); instance.CurrentStage = stage; db.WfRequestInstances.Add(instance);
            db.RequestTypesAllowedRequestStateTransitions.AddRange(new() { RequestTypeId = 900, FromRequestStateId = 2, ToRequestStateId = twoStages ? 7 : 4 },
                new() { RequestTypeId = 900, FromRequestStateId = 7, ToRequestStateId = 4 }, new() { RequestTypeId = 900, FromRequestStateId = 2, ToRequestStateId = 5 },
                new() { RequestTypeId = 900, FromRequestStateId = 2, ToRequestStateId = 6 });
            db.CommunicationProviderConnections.Add(new() { CommunicationProviderConnectionId = 1, OrganisationBusinessEntityId = 10, ProviderCode = "TELEGRAM", IsEnabled = true });
            db.CommunicationDestinations.Add(new() { CommunicationDestinationId = 1, OrganisationBusinessEntityId = 10, CommunicationProviderConnectionId = 1, ExternalDestinationId = "-100123", DisplayName = "Original channel", IsActive = true });
            db.OrganisationEvents.Add(new() { OrganisationEventId = 1, OrganisationBusinessEntityId = 10, Title = "Holiday", Status = "Published", Audiences = [new() { AudienceType = "Organisation", IsIncluded = true }] });
            db.CommunicationOutbox.Add(new() { CommunicationOutboxId = 1, EventId = Guid.NewGuid(), OrganisationBusinessEntityId = 10, EventCode = CommunicationEventCodes.OrganisationEventPublished, AggregateType = "OrganisationEvent", AggregateId = "1", PayloadJson = "{\"OrganisationWideAudience\":true}" });
            db.CommunicationMessages.Add(new() { CommunicationMessageId = 1, OrganisationBusinessEntityId = 10, ApprovalStatus = "Pending", ApprovalRequestId = 1, ApprovalJobId = 10,
                CommunicationOutboxId = 1, CommunicationDestinationId = 1, DestinationConnectionId = 1, DestinationExternalId = "-100123", DestinationDisplayName = "Original channel",
                CommunicationAutomationRuleId = 1, Subject = "Original subject", RenderedBody = "Original saved body", CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        public Task Act(string action = "Approve", int state = 2) => Service.ActOnAnnouncementAsync(new() { Id = 1, ExpectedState = state, Action = action, Comment = "Reviewed" });
    }

    [Fact]
    public async Task OnlyFinalStageQueuesSavedMessageAndDuplicateActionIsRejected()
    {
        var f = new Fixture(); await f.Seed(twoStages: true);
        Assert.True((await f.Service.LoadApprovalsAsync()).Single().CanApprove);
        await f.Act();
        await using (var db = f.Factory.CreateDbContext()) { Assert.Empty(await db.CommunicationDeliveries.ToListAsync()); Assert.Equal("Pending", (await db.CommunicationMessages.SingleAsync()).ApprovalStatus); }
        f.Access.Actor!.UserId = 2; f.Access.Actor.IndividualId = 300;
        await f.Act(state: 7);
        await using (var db = f.Factory.CreateDbContext())
        {
            var delivery = await db.CommunicationDeliveries.SingleAsync();
            Assert.Equal(1, delivery.CommunicationDestinationId);
            Assert.Equal("Pending", delivery.Status);
            var message = await db.CommunicationMessages.SingleAsync();
            Assert.Equal("Approved", message.ApprovalStatus); Assert.Equal("Original saved body", message.RenderedBody);
            Assert.Equal("Approved", (await db.WfRequestInstances.SingleAsync()).WorkflowStatus);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Act(state: 7));
    }

    [Theory]
    [InlineData("Reject", "Rejected")]
    [InlineData("Cancel", "Cancelled")]
    public async Task RejectOrSubmitterCancelNeverQueuesDelivery(string action, string status)
    {
        var f = new Fixture(); await f.Seed();
        if (action == "Cancel") { f.Access.Actor!.UserId = 3; f.Access.Actor.IndividualId = 200; }
        await f.Act(action);
        await using var db = f.Factory.CreateDbContext();
        Assert.Empty(await db.CommunicationDeliveries.ToListAsync()); Assert.Equal(status, (await db.CommunicationMessages.SingleAsync()).ApprovalStatus);
    }

    [Fact]
    public async Task SelfApprovalUnassignedReviewerAndOtherOrganisationAreBlocked()
    {
        var f = new Fixture(); await f.Seed();
        f.Access.Actor!.IndividualId = 200;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Act());
        f.Access.Actor.IndividualId = 999; f.Access.Actor.UserId = 999;
        Assert.Empty(await f.Service.LoadApprovalsAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Act());
        f.Access.Organisation = 20;
        Assert.Empty(await f.Service.LoadApprovalsAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Act());
    }

    [Theory]
    [InlineData("destination")]
    [InlineData("source")]
    [InlineData("expiry")]
    public async Task ChangedDestinationCancelledSourceOrExpiredSnapshotCannotRelease(string change)
    {
        var f = new Fixture(); await f.Seed();
        await using (var db = f.Factory.CreateDbContext())
        {
            if (change == "destination") (await db.CommunicationDestinations.SingleAsync()).ExternalDestinationId = "-100999";
            if (change == "source") (await db.OrganisationEvents.SingleAsync()).Status = "Cancelled";
            if (change == "expiry") (await db.CommunicationMessages.SingleAsync()).CreatedAtUtc = DateTime.UtcNow.AddHours(-25);
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Act());
        await using var check = f.Factory.CreateDbContext(); Assert.Empty(await check.CommunicationDeliveries.ToListAsync());
    }

    [Fact]
    public async Task StaleDecisionAndNonSubmitterCancellationAreDenied()
    {
        var f = new Fixture(); await f.Seed();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Act(state: 7));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Act("Cancel"));
        await using var db = f.Factory.CreateDbContext();
        Assert.Empty(await db.CommunicationDeliveries.ToListAsync());
        Assert.Equal("Pending", (await db.CommunicationMessages.SingleAsync()).ApprovalStatus);
    }

    [Fact]
    public async Task SpecificRequestLookupStillEnforcesAssignment()
    {
        var f = new Fixture(); await f.Seed();
        Assert.Single(await f.Service.LoadApprovalsAsync(1));
        Assert.Empty(await f.Service.LoadApprovalsAsync(999));
        f.Access.Actor!.UserId = 999; f.Access.Actor.IndividualId = 999;
        Assert.Empty(await f.Service.LoadApprovalsAsync(1));
    }
}
