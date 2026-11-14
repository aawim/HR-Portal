using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Services;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class WorkforceScheduleTests
{
    [Fact]
    public async Task RosterCreation_RejectsAnInvertedPeriod()
    {
        var service = new WorkforceRosterService(new QueryFactory(), Availability(), Constraints());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDraftAsync(new(10, "Invalid", new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 1), 1, 1)));
    }

    [Fact]
    public async Task LedgerRecording_IsIdempotentForTheSameSourceAndCategory()
    {
        var service = new WorkTimeLedgerService(new QueryFactory());
        var entry = new RecordWorkTimeLedgerEntry(10, 100, 200, DateTime.UtcNow, DateTime.UtcNow.AddHours(8), "Attendance", 480, 480, "WS_SESSION:1", 1);
        await service.RecordAsync(entry);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordAsync(entry));
    }

    [Fact]
    public async Task RosterValidation_ReportsCoverageOverlapAndLeave()
    {
        var factory = new QueryFactory();
        var start = new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc);
        using (var db = factory.CreateDbContext())
        {
            var roster = new WorkforceRoster { OrganisationId = 10, Name = "Coverage", StartsOn = start.Date, EndsOn = start.Date, OperationLogId = 1 };
            var version = new WorkforceRosterVersion { WorkforceRoster = roster, VersionNumber = 1, CreatedByUserId = 1, CreatedAtUtc = DateTime.UtcNow, OperationLogId = 1 };
            db.WorkforceRosterVersions.Add(version);
            await db.SaveChangesAsync();
            db.DutyOccurrences.AddRange(
                new DutyOccurrence { WorkforceRosterVersionId = version.Id, DutyTypeId = 1, StartAt = start, EndAt = start.AddHours(8), RequiredStaffCount = 2, TimeZoneId = "UTC", OperationLogId = 1 },
                new DutyOccurrence { WorkforceRosterVersionId = version.Id, DutyTypeId = 1, StartAt = start.AddHours(4), EndAt = start.AddHours(12), RequiredStaffCount = 1, TimeZoneId = "UTC", OperationLogId = 1 });
            await db.SaveChangesAsync();
            var duties = db.DutyOccurrences.OrderBy(x => x.Id).ToList();
            db.DutyAssignments.AddRange(new DutyAssignment { DutyOccurrenceId = duties[0].Id, JobId = 200, IndividualId = 100, Status = "Assigned", OperationLogId = 1 }, new DutyAssignment { DutyOccurrenceId = duties[1].Id, JobId = 200, IndividualId = 100, Status = "Assigned", OperationLogId = 1 });
            await db.SaveChangesAsync();
        }
        var unavailable = StubProxy.For<IDutyAvailabilityService>((_, _) => Task.FromResult(new DutyAvailabilityAssessment(false, "Approved leave overlaps this duty.")));
        var service = new WorkforceRosterService(factory, unavailable, Constraints());
        var result = await service.ValidateAsync(1, 10);
        Assert.NotEmpty(result.CoverageIssues);
        Assert.NotEmpty(result.OverlapIssues);
        Assert.NotEmpty(result.LeaveIssues);
    }

    [Fact]
    public async Task PayrollExport_LocksEntriesAndRejectsAReusedReference()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        using (var db = factory.CreateDbContext())
        {
            db.WorkTimeLedgerEntries.Add(new WorkTimeLedgerEntry { OrganisationId = 10, IndividualId = 100, JobId = 200, StartAt = DateTime.UtcNow.AddHours(-8), EndAt = DateTime.UtcNow, TimeCategory = "Attendance", ActualMinutes = 480, CreditedMinutes = 480, SourceReference = "seed", Status = "Calculated", CreatedAtUtc = DateTime.UtcNow, OperationLogId = 1 });
            await db.SaveChangesAsync();
        }
        var service = new WorkforcePayrollExportService(factory);
        var rows = await service.PrepareAsync(10, "PAY-2026-001");
        Assert.Single(rows);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PrepareAsync(10, "PAY-2026-001"));
        using var verify = factory.CreateDbContext();
        var saved = Assert.Single(verify.WorkTimeLedgerEntries);
        Assert.True(saved.IsPayrollFinalized);
        Assert.Equal("PAY-2026-001", saved.PayrollExportReference);
    }

    [Fact]
    public async Task CompletedSession_RecordsExactlyOneLedgerRequest()
    {
        var factory = new QueryFactory();
        using (var db = factory.CreateDbContext())
        {
            var plan = new WorkPlan { WorkPlanId = 1, IndividualId = 100, JobId = 200, OrganisationBusinessEntityId = 10, WorkDate = DateTime.UtcNow.Date, GeneratedDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, PlanGuid = Guid.NewGuid() };
            db.WorkPlanSegments.Add(new WorkPlanSegment { WorkPlanSegmentId = 1, WorkPlan = plan, WorkPlanId = 1, WorkSegmentTypeId = 1, Name = "Duty", StartDateTime = DateTime.UtcNow.AddHours(-8), EndDateTime = DateTime.UtcNow, OperationLogId = 1, CreatedDate = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var calls = 0;
        var ledger = StubProxy.For<IWorkTimeLedgerService>((_, _) => { calls++; return Task.FromResult(1L); });
        var service = new WorkforceAttendanceSessionService(factory, ledger);
        await service.EvaluateAsync(new(1, null, "Completed", "CheckInOut", 480, 480, "matched", 1));
        Assert.Equal(1, calls);
    }

    private static IDutyAvailabilityService Availability() => StubProxy.For<IDutyAvailabilityService>((_, _) => Task.FromResult(new DutyAvailabilityAssessment(true, null)));
    private static IWorkforceConstraintService Constraints() => StubProxy.For<IWorkforceConstraintService>((_, _) => Task.FromResult<IReadOnlyList<WorkforceConstraintViolation>>([]));
}
