using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Services;
using HRM.WorkPlanning.Services.Schedules;
using HRM.WorkPlanning.Results;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class SchedulingSafetyTests
{
    private static IDutyAvailabilityService Available() => StubProxy.For<IDutyAvailabilityService>((_, _) => Task.FromResult(new DutyAvailabilityAssessment(true, null)));
    private static IWorkforceConstraintService NoConstraints() => StubProxy.For<IWorkforceConstraintService>((_, _) => Task.FromResult<IReadOnlyList<WorkforceConstraintViolation>>([]));

    [Fact]
    public async Task StaffScheduleDeniesAnotherEmployeeWithoutStaffAccess()
    {
        var service = new StaffScheduleService(new QueryFactory(), new FakeAccess());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetScheduleAsync(200));
    }

    [Fact]
    public async Task ActiveScheduleIncludesFutureTerminationButExcludesFutureEmployment()
    {
        var factory = new QueryFactory(); var today = DateTime.UtcNow.AddHours(5).Date;
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new() { BusinessEntityId = 100, FirstNameEnglish = "Employee" });
            db.Organisations.Add(new() { BusinessEntityID = 10, OrganisationName = "Tenant" });
            db.Jobs.AddRange(new() { JobId = 1, IndividualID = 100, OrganisationID = 10, JobStateId = 4, JoinedDate = today.AddMonths(-1), TerminatedDate = today.AddDays(10) },
                new() { JobId = 2, IndividualID = 100, OrganisationID = 10, JobStateId = 4, JoinedDate = today.AddDays(2) },
                new() { JobId = 3, IndividualID = 100, OrganisationID = 10, JobStateId = 4, JoinedDate = today.AddDays(-1), TerminatedDate = today });
            await db.SaveChangesAsync();
        }
        var result = await new StaffScheduleService(factory, new FakeAccess()).GetScheduleAsync(100);
        Assert.True(result.Success); Assert.Equal(1, result.Schedule!.JobId);
    }

    private static async Task SeedRosters(QueryFactory factory, DateTime start, bool publishedNeighbour = false)
    {
        await using var db = factory.CreateDbContext();
        var roster = new WorkforceRoster { Id = 1, OrganisationId = 10, Name = "Draft", StartsOn = start.Date, EndsOn = start.Date.AddDays(3) };
        var version = new WorkforceRosterVersion { Id = 1, WorkforceRoster = roster };
        var occurrence = new DutyOccurrence { Id = 1, WorkforceRosterVersion = version, DutyTypeId = 1, StartAt = start, EndAt = start.AddHours(4) };
        db.DutyTypes.Add(new() { Id = 1, OrganisationId = 10, Code = "DUTY", Name = "Duty", IsActive = true, WorkTemplateId = 1 });
        db.DutyAssignments.Add(new() { Id = 1, DutyOccurrence = occurrence, JobId = 200, IndividualId = 100 });
        db.Jobs.Add(new() { JobId = 200, IndividualID = 100, OrganisationID = 10, JobStateId = 4, JoinedDate = start.AddYears(-1) });
        if (publishedNeighbour)
        {
            var other = new WorkforceRoster { Id = 2, OrganisationId = 10, Name = "Published", StartsOn = start.Date, EndsOn = start.Date.AddDays(3), Status = "Published" };
            var otherVersion = new WorkforceRosterVersion { Id = 2, WorkforceRoster = other, Status = "Published" };
            db.DutyAssignments.Add(new() { Id = 2, JobId = 200, IndividualId = 100, DutyOccurrence = new() { Id = 2, WorkforceRosterVersion = otherVersion, DutyTypeId = 1, StartAt = start.AddHours(-2), EndAt = start.AddHours(1) } });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ValidationDetectsOverlapWithAnotherPublishedRoster()
    {
        var factory = new QueryFactory(); await SeedRosters(factory, DateTime.UtcNow.Date.AddHours(10), true);
        var result = await new WorkforceRosterService(factory, Available(), NoConstraints()).ValidateAsync(1, 10);
        Assert.False(result.IsValid); Assert.Contains(result.OverlapIssues, x => x.Contains("published duty"));
    }

    [Fact]
    public async Task RestPolicyIncludesPreviousPublishedRoster()
    {
        var factory = new QueryFactory(); var start = DateTime.UtcNow.Date.AddHours(10); await SeedRosters(factory, start, true);
        await using (var db = factory.CreateDbContext()) { (await db.DutyOccurrences.SingleAsync(x => x.Id == 2)).EndAt = start.AddHours(-1); await db.SaveChangesAsync(); }
        var policies = StubProxy.For<IWorkforcePolicyResolver>((_, _) => Task.FromResult<IReadOnlyList<ResolvedWorkforcePolicy>>([new(1, "REST", "{\"MIN_REST_BETWEEN_SHIFTS\":{\"minimumMinutes\":600}}", 1, 1)]));
        var results = await new WorkforceConstraintService(factory, policies).ValidateAsync(1);
        Assert.Contains(results, x => x.Code == "MIN_REST_BETWEEN_SHIFTS");
    }

    [Fact]
    public async Task PublicationPreventsAnEditFromSlippingPastValidation()
    {
        var factory = new QueryFactory(ignoreTransactions: true); var start = DateTime.UtcNow.Date.AddHours(10); await SeedRosters(factory, start);
        var validating = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<DutyAvailabilityAssessment> Pause() { validating.TrySetResult(); await release.Task; return new(true, null); }
        var availability = StubProxy.For<IDutyAvailabilityService>((_, _) => Pause());
        var service = new WorkforceRosterService(factory, availability, NoConstraints());
        var publish = service.PublishAsync(1, 1, 1);
        await validating.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var edit = service.AddOccurrenceAsync(new(1, 10, 1, start.AddHours(6), start.AddHours(8), 1, "Indian/Maldives", null, null, 1));
        try { Assert.False(edit.IsCompleted); }
        finally { release.SetResult(); }
        await publish;
        await Assert.ThrowsAsync<InvalidOperationException>(() => edit);
        await using var check = factory.CreateDbContext(); Assert.Single(await check.DutyOccurrences.ToListAsync());
    }

    [Fact]
    public async Task OvernightRosterUsesActualEndAndIsNotRepeatedTheNextDay()
    {
        var factory = new QueryFactory(ignoreTransactions: true); var start = DateTime.UtcNow.Date.AddHours(22); await SeedRosters(factory, start);
        await using (var db = factory.CreateDbContext())
        {
            var version = await db.WorkforceRosterVersions.Include(x => x.WorkforceRoster).SingleAsync(); version.Status = "Published"; version.WorkforceRoster.Status = "Published";
            (await db.DutyOccurrences.SingleAsync()).EndAt = start.AddHours(5);
            db.WorkAssignmentStates.Add(new() { WorkAssignmentStateId = 1, Code = "PLANNED", Name = "Planned", IsActive = true });
            db.WorkSegmentTypes.Add(new() { WorkSegmentTypeId = 1, Code = "CHECK_IN", Name = "Work", IsActive = true });
            db.WorkTemplates.Add(new() { WorkTemplateId = 1, OrganisationBusinessEntityId = 10, Name = "Eight-hour template", IsActive = true, DefaultStartTime = new(8, 0), DefaultEndTime = new(16, 0),
                WorkTemplateSegments = [new() { WorkTemplateSegmentId = 1, WorkSegmentTypeId = 1, Name = "Work", SequenceNumber = 1, DurationMinutes = 480, IsActive = true }] });
            await db.SaveChangesAsync();
        }
        var provider = StubProxy.For<IPlanningProviderResolver>((_, _) => Task.FromResult(new PlanningProviderResolution { PlanningProviderId = 1, Code = "WORK_PLANNING" }));
        var generator = new WorkPlanGenerator(factory, provider);
        var first = await generator.GenerateOrGetAsync(100, 200, 10, DateOnly.FromDateTime(start));
        Assert.NotNull(first); var assignment = Assert.Single(first.Assignments);
        Assert.Equal(start, assignment.StartDateTime); Assert.Equal(start.AddHours(5), assignment.EndDateTime);
        await using (var db = factory.CreateDbContext()) { (await db.DutyOccurrences.SingleAsync()).EndAt = start.AddHours(6); await db.SaveChangesAsync(); }
        var refreshed = await generator.GenerateOrGetAsync(100, 200, 10, DateOnly.FromDateTime(start));
        Assert.Equal(start.AddHours(6), Assert.Single(refreshed!.Assignments).EndDateTime);
        Assert.Null(await generator.GenerateOrGetAsync(100, 200, 10, DateOnly.FromDateTime(start.AddDays(1))));
    }

    [Fact]
    public async Task ReviewRequiresPermissionAndPagesOnlyVisibleEmployees()
    {
        var factory = new QueryFactory(); var access = new FakeAccess(); var now = DateTime.UtcNow.Date.AddHours(2);
        await using (var db = factory.CreateDbContext())
        {
            db.UserAccessScopes.Add(new() { UserAccessScopeId = 1, OrganisationId = 10, UserId = 1, ScopeType = "Self", IsActive = true, ValidFrom = DateTime.UtcNow.AddDays(-1), Reason = "Self-only review" });
            db.WorkTimeLedgerEntries.AddRange(new() { Id = 1, OrganisationId = 10, IndividualId = 100, JobId = 1, StartAt = now, EndAt = now.AddHours(1), SourceReference = "one" },
                new() { Id = 2, OrganisationId = 10, IndividualId = 100, JobId = 1, StartAt = now.AddMinutes(1), EndAt = now.AddHours(1), SourceReference = "two" },
                new() { Id = 3, OrganisationId = 10, IndividualId = 200, JobId = 2, StartAt = now, EndAt = now.AddHours(1), SourceReference = "hidden" },
                new() { Id = 4, OrganisationId = 20, IndividualId = 100, JobId = 3, StartAt = now, EndAt = now.AddHours(1), SourceReference = "other-tenant" });
            await db.SaveChangesAsync();
        }
        var service = new WorkforceScheduleReviewService(factory, access);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetLedgerPageAsync(10, new()));
        access.Permissions.Add("Payroll.View");
        var first = await service.GetLedgerPageAsync(10, new(PageSize: 1)); var second = await service.GetLedgerPageAsync(10, new(Page: 2, PageSize: 1));
        Assert.Equal(2, first.TotalCount); Assert.Equal(2, Assert.Single(first.Items).Id); Assert.Equal(1, Assert.Single(second.Items).Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetLedgerPageAsync(20, new()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetLedgerPageAsync(10, new(PageSize: 101)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetLedgerPageAsync(10, new(From: now.AddYears(-2), To: now)));
        Assert.Empty((await service.GetLedgerPageAsync(10, new(From: now.AddDays(1), To: now.AddDays(2)))).Items);
    }

    [Fact]
    public async Task SessionReviewUsesAttendancePermissionIndependentlyOfPayroll()
    {
        var access = new FakeAccess(); access.Permissions.Add("Payroll.View");
        var service = new WorkforceScheduleReviewService(new QueryFactory(), access);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSessionsPageAsync(10, new()));
        access.Permissions.Add("Attendance.View");
        Assert.Empty((await service.GetSessionsPageAsync(10, new())).Items);
    }
}
