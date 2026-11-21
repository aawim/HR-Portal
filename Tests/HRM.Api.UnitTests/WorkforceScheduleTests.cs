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

    [Fact]
    public async Task RotatingShiftSchedule_SupportsPerWeekdayCustomTimeWindows_AndRepeatsWeekly()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var sunday = new DateTime(2026, 11, 22); // Sunday
        var slots = new List<HRM.Helpers.RotationSlot>
        {
            new(1, new TimeOnly(10, 0), new TimeOnly(12, 0), "Morning"),
            new(0),
            new(1, new TimeOnly(14, 0), new TimeOnly(16, 0), "Evening"),
            new(0),
            new(0),
            new(0),
            new(0)
        };
        var patternJson = HRM.Helpers.SchedulePattern.SerializeSlots(slots);

        using (var db = factory.CreateDbContext())
        {
            db.WorkAssignmentStates.Add(new WorkAssignmentState { WorkAssignmentStateId = 1, Code = "PLANNED", Name = "Planned", IsActive = true });
            db.WorkTemplateTypes.Add(new WorkTemplateType { WorkTemplateTypeId = 1, Code = "SHIFT", Name = "Shift", IsActive = true });
            db.WorkSegmentTypes.Add(new WorkSegmentType { WorkSegmentTypeId = 1, Code = "CHECK_IN", Name = "Shift Work", IsActive = true });
            db.WorkTemplates.Add(new WorkTemplate
            {
                WorkTemplateId = 1,
                WorkTemplateTypeId = 1,
                OrganisationBusinessEntityId = 10,
                Name = "Flexible Shift",
                DefaultStartTime = new TimeOnly(8, 0),
                DefaultEndTime = new TimeOnly(16, 0),
                RequiresAttendance = true,
                RequiresCheckOut = true,
                IsActive = true
            });
            db.WorkTemplateSegments.Add(new WorkTemplateSegment
            {
                WorkTemplateSegmentId = 1,
                WorkTemplateId = 1,
                WorkSegmentTypeId = 1,
                Name = "Duty Window",
                SequenceNumber = 1,
                OffsetMinutes = 0,
                DurationMinutes = 480,
                IsMandatory = true,
                RequiresAttendance = true,
                IsActive = true,
                IsPaid = true,
                OperationLogId = 1
            });
            db.Jobs.Add(new Job { JobId = 200, IndividualID = 100, OrganisationID = 10, OrganisationStructureId = 1, JobStateId = 4, JoinedDate = sunday.AddMonths(-1) });
            db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment
            {
                JobWorkTemplateAssignmentID = 1,
                JobID = 200,
                WorkTemplateID = 1,
                SchedulePurpose = "Official",
                ScheduleMode = "Rotation",
                WorkingDaysMask = 127,
                RotationAnchorDate = sunday,
                RotationTemplateIds = patternJson,
                EffectiveFrom = sunday,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var provider = StubProxy.For<IPlanningProviderResolver>((_, _) =>
            Task.FromResult(new HRM.WorkPlanning.Results.PlanningProviderResolution { PlanningProviderId = 1, Code = "WORK_PLANNING" }));
        var generator = new HRM.WorkPlanning.WorkPlanGenerator(factory, provider);

        var sunPlan = await generator.GenerateOrGetAsync(100, 200, 10, DateOnly.FromDateTime(sunday));
        Assert.NotNull(sunPlan);
        var sunAssignment = Assert.Single(sunPlan!.Assignments);
        Assert.Equal(sunday.AddHours(10), sunAssignment.StartDateTime);
        Assert.Equal(sunday.AddHours(12), sunAssignment.EndDateTime);
        Assert.Contains("Morning", sunAssignment.Name);
        var sunSeg = Assert.Single(sunPlan.Segments);
        Assert.Equal(sunday.AddHours(10), sunSeg.StartDateTime);
        Assert.Equal(sunday.AddHours(12), sunSeg.EndDateTime);

        var monPlan = await generator.GenerateOrGetAsync(100, 200, 10, DateOnly.FromDateTime(sunday.AddDays(1)));
        Assert.Null(monPlan);

        var tue = sunday.AddDays(2);
        var tuePlan = await generator.GenerateOrGetAsync(100, 200, 10, DateOnly.FromDateTime(tue));
        Assert.NotNull(tuePlan);
        var tueAssignment = Assert.Single(tuePlan!.Assignments);
        Assert.Equal(tue.AddHours(14), tueAssignment.StartDateTime);
        Assert.Equal(tue.AddHours(16), tueAssignment.EndDateTime);
        Assert.Contains("Evening", tueAssignment.Name);

        var nextSunday = sunday.AddDays(7);
        var nextSunPlan = await generator.GenerateOrGetAsync(100, 200, 10, DateOnly.FromDateTime(nextSunday));
        Assert.NotNull(nextSunPlan);
        var nextSunAssignment = Assert.Single(nextSunPlan!.Assignments);
        Assert.Equal(nextSunday.AddHours(10), nextSunAssignment.StartDateTime);
        Assert.Equal(nextSunday.AddHours(12), nextSunAssignment.EndDateTime);
    }

    [Fact]
    public void RotatingShiftSchedule_PlainTemplateIds_RemainsBackwardCompatible()
    {
        var slots = new List<HRM.Helpers.RotationSlot> { new(1), new(2), new(0), new(0) };
        var json = HRM.Helpers.SchedulePattern.SerializeSlots(slots);
        Assert.Equal("[1,2,0,0]", json);
        Assert.Equal(new[] { 1, 2, 0, 0 }, HRM.Helpers.SchedulePattern.Parse(json));
    }

    private static IDutyAvailabilityService Availability() => StubProxy.For<IDutyAvailabilityService>((_, _) => Task.FromResult(new DutyAvailabilityAssessment(true, null)));
    private static IWorkforceConstraintService Constraints() => StubProxy.For<IWorkforceConstraintService>((_, _) => Task.FromResult<IReadOnlyList<WorkforceConstraintViolation>>([]));
}
