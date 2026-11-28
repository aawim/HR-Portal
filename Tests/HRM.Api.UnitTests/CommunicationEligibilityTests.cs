using HRM.Communications.Contracts;
using HRM.Models;
using HRM.Services.Communications;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CommunicationEligibilityTests
{
    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"JobId\":\"bad\"}")]
    [InlineData("not-json")]
    public async Task InvalidSchedulePayloadIsIneligible(string json)
    {
        await using var db = new QueryFactory().CreateDbContext();
        Assert.False(await CommunicationEligibility.SourceIsEligibleAsync(db, new() { AggregateType = "EmployeeSchedule", EventCode = CommunicationEventCodes.EmployeeScheduleChanged, PayloadJson = json }));
    }

    [Fact]
    public async Task PrivateSchedulesRequireMatchingEmployeeAndActiveConsentAtDispatch()
    {
        await using var db = new QueryFactory().CreateDbContext();
        db.Jobs.Add(new() { JobId = 1, IndividualID = 100, OrganisationID = 10, JobStateId = 4 });
        db.ScopeScheduleAssignments.Add(new() { Id = 1, OrganisationId = 10, Version = 2 });
        await db.SaveChangesAsync();
        var source = new CommunicationOutbox { OrganisationBusinessEntityId = 10, AggregateType = "EmployeeSchedule", EventCode = CommunicationEventCodes.EmployeeScheduleChanged,
            PayloadJson = "{\"JobId\":1,\"IndividualId\":100,\"ScopeScheduleAssignmentId\":1,\"ScopeVersion\":2}" };
        var destination = new CommunicationDestination { CommunicationDestinationId = 1, OrganisationBusinessEntityId = 10, IndividualId = 100 };
        Assert.False(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "WHATSAPP"));
        var consent = new CommunicationConsent { OrganisationBusinessEntityId = 10, CommunicationDestinationId = 1, IndividualId = 100, Source = "Signed opt-in", ConsentedAtUtc = DateTime.UtcNow };
        db.CommunicationConsents.Add(consent); await db.SaveChangesAsync();
        Assert.True(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "WHATSAPP"));
        Assert.False(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "TELEGRAM"));
        destination.IndividualId = 200;
        Assert.False(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "WHATSAPP"));
        destination.IndividualId = 100; consent.RevokedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync();
        Assert.False(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "WHATSAPP"));
    }

    [Fact]
    public async Task FacebookRequiresPublicSnapshotAndCurrentVisibility()
    {
        await using var db = new QueryFactory().CreateDbContext();
        var item = new OrganisationEvent { OrganisationEventId = 1, OrganisationBusinessEntityId = 10, Title = "Event", Status = "Published", Visibility = "Public",
            Audiences = [new() { AudienceType = "Organisation", IsIncluded = true }] };
        db.OrganisationEvents.Add(item); await db.SaveChangesAsync();
        var source = new CommunicationOutbox { OrganisationBusinessEntityId = 10, AggregateType = "OrganisationEvent", AggregateId = "1", EventCode = CommunicationEventCodes.OrganisationEventPublished,
            PayloadJson = "{\"OrganisationWideAudience\":true,\"PublicAnnouncement\":true}" };
        var destination = new CommunicationDestination { OrganisationBusinessEntityId = 10 };
        Assert.True(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "FACEBOOK_PAGE"));
        item.Visibility = "Audience"; await db.SaveChangesAsync();
        Assert.False(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "FACEBOOK_PAGE"));
        Assert.True(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "MSTEAMS"));
        destination.OrganisationBusinessEntityId = 20;
        Assert.False(await CommunicationEligibility.CanDeliverToAsync(db, source, destination, "MSTEAMS"));
    }
}
