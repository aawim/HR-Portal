using HRM.DTOs.Attendance;
using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.WorkPlanning;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

static class WorkingHoursTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static async Task Denied(Func<Task> action, string message)
    {
        try { await action(); } catch (UnauthorizedAccessException) { return; } catch (InvalidOperationException) { return; }
        throw new Exception(message);
    }
    public static async Task RunAsync()
    {
        await TemporaryHoursAsync();
        await RainAsync();
        await TotalsAsync();
        await EligibilityAsync();
        await ConfigurationAsync();
        await ApprovalsAsync();
    }

    private static async Task TemporaryHoursAsync()
    {
        var f = await Fixture.CreateAsync();
        await f.DayAsync(f.Sunday, 8, 15, 16, 0);
        await using var db = f.Factory.CreateDbContext();
        db.JobTypeWorkingHourRules.RemoveRange(db.JobTypeWorkingHourRules);
        await db.SaveChangesAsync();
        var normal = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
        Check(normal.RequiredWeekHours == 30 && normal.HasWorkingHoursRule, "Normal hours must derive from the template without a manual weekly rule");
        db.OrganisationWorkingHourRequirements.Add(new() { OrganisationId = 10, JobTypeId = 1, FromDate = f.Sunday, ToDate = f.Sunday.AddDays(6), RequiredDailyMinutes = 480, Reason = "Temporary extended week" });
        db.OrganisationWorkingHourRequirements.Add(new() { OrganisationId = 10, FromDate = f.Sunday, ToDate = f.Sunday.AddDays(6), RequiredDailyMinutes = 420, Reason = "All-types fallback" });
        db.OrganisationWorkingHourRequirements.Add(new() { OrganisationId = 10, JobTypeId = 2, FromDate = f.Sunday, ToDate = f.Sunday.AddDays(6), RequiredDailyMinutes = 600, Reason = "Different Job Type" });
        db.OrganisationWorkingHourRequirements.Add(new() { OrganisationId = 20, FromDate = f.Sunday, ToDate = f.Sunday.AddDays(6), RequiredDailyMinutes = 600, Reason = "Other tenant" });
        await db.SaveChangesAsync();
        var extended = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
        Check(extended.RequiredWeekHours == 40, "8 hours must apply per working day, not per week");
        var facts = await AttendanceScheduleFacts.LoadAsync(db, 100, f.Sunday, f.Sunday, 10);
        Check(facts.Single().ScheduledEnd == f.Sunday.AddHours(16), "Temporary hours failed to extend official finish");
        db.OrganisationScheduleAmendments.Add(new() { OrganisationId = 10, WorkDate = f.Sunday, OriginalStart = TimeSpan.FromHours(8), RevisedStart = TimeSpan.FromHours(8.5), Reason = "Rain" });
        await db.SaveChangesAsync();
        var rain = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
        Check(rain.RequiredWeekHours == 39.5m, "Rain must reduce extended hours without make-up");
        var evaluation = await new HRM.Services.Attendance.AttendanceEvaluationService(f.Factory).EvaluateAsync(100, 1, f.Sunday);
        Check(evaluation.PlannedStart == f.Sunday.AddHours(8.5) && evaluation.PlannedEnd == f.Sunday.AddHours(16) && evaluation.LateMinutes == 0, "Evaluation failed to compose extended hours and rain waiver");
        Check((await db.WorkAssignments.SingleAsync()).EndDateTime == f.Sunday.AddHours(14), "Temporary hours rewrote the dated plan");
        var rows = await db.OrganisationWorkingHourRequirements.Where(r => r.OrganisationId == 10).ToListAsync(); foreach (var row in rows) row.IsActive = false; await db.SaveChangesAsync();
        Check((await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday)).RequiredWeekHours == 29.5m, "Withdrawal failed to restore template hours");
        var future = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday.AddDays(7));
        Check(future.RequiredWeekHours == 30, "Temporary hours leaked outside the selected period");
    }

    private static async Task RainAsync()
    {
        foreach (var minute in new[] { 15, 30, 31 })
        {
            var f = await Fixture.CreateAsync();
            await f.DayAsync(f.Sunday, 8, minute, 14, 0);
            await using (var db = f.Factory.CreateDbContext())
            {
                db.OrganisationScheduleAmendments.Add(new() { OrganisationId = 10, WorkDate = f.Sunday,
                    OriginalStart = TimeSpan.FromHours(8), RevisedStart = TimeSpan.FromHours(8.5), Reason = "Rain" });
                // Another tenant's delay must not change this employee's effective schedule.
                db.OrganisationScheduleAmendments.Add(new() { OrganisationId = 20, WorkDate = f.Sunday,
                    OriginalStart = TimeSpan.FromHours(8), RevisedStart = TimeSpan.FromHours(10), Reason = "Other tenant" });
                await db.SaveChangesAsync();
            }
            var day = (await f.Attendance.GetMyYearlyAttendanceAsync(2026)).Single(d => d.Date == f.Sunday);
            Check(day.ScheduledStart == f.Sunday.AddHours(8.5) && day.ScheduledEnd == f.Sunday.AddHours(14), "Rain changed the official end or failed to change the start");
            Check(day.IsLate == (minute > 30) && day.LateMinutes == (minute > 30 ? 1 : 0), $"Rain lateness incorrect for 08:{minute}");
            Check(day.IsPerfectlyAttended == (minute <= 30), "Amended on-time attendance did not count as perfectly attended");
            var evaluation = await new HRM.Services.Attendance.AttendanceEvaluationService(f.Factory).EvaluateAsync(100, 1, f.Sunday);
            Check(evaluation.PlannedStart == f.Sunday.AddHours(8.5) && evaluation.LateMinutes == day.LateMinutes,
                "Daily evaluation disagrees with yearly attendance after rain amendment");
            await using var db2 = f.Factory.CreateDbContext();
            var weekly = await WeeklyWorkingHours.LoadAsync(db2, 100, 10, f.Sunday);
            Check(weekly.RequiredWeekHours == 29.5m && weekly.AmendmentCreditHours == .5m, "Rain did not waive 30 minutes from weekly target");
            Check((await db2.AttendanceLogs.OrderBy(l => l.Date).FirstAsync()).Date == f.Sunday.AddHours(8).AddMinutes(minute), "Amendment rewrote the physical clock time");
            Check((await db2.WorkAssignments.SingleAsync()).StartDateTime == f.Sunday.AddHours(8), "Read overlay rewrote the dated assignment");
            var amendment = await db2.OrganisationScheduleAmendments.SingleAsync(a => a.OrganisationId == 10); amendment.IsActive = false; await db2.SaveChangesAsync();
            var withdrawn = (await f.Attendance.GetMyYearlyAttendanceAsync(2026)).Single(d => d.Date == f.Sunday);
            Check(withdrawn.ScheduledStart == f.Sunday.AddHours(8) && withdrawn.IsLate, "Withdrawing an amendment did not restore the original start");
        }
        var dutyStart = new DateTime(2026, 1, 4, 8, 0, 0);
        var amendments = new List<OrganisationScheduleAmendment> { new() { OrganisationId = 10, WorkDate = dutyStart.Date,
            OriginalStart = TimeSpan.FromHours(8), RevisedStart = TimeSpan.FromHours(8.5) } };
        Check(OfficialScheduleAmendments.Start(amendments, 10, "Duty", dutyStart) == dutyStart, "Rain shifted duty hours");
        Check(OfficialScheduleAmendments.Start(amendments, 20, "Official", dutyStart) == dutyStart, "Rain crossed the tenant boundary");
        Check(OfficialScheduleAmendments.Segment(amendments, 10, "Official", "CHECK_OUT", dutyStart, dutyStart.AddHours(1), 0).Start == dutyStart,
            "An amendment shifted a check-out event");
    }

    private static async Task TotalsAsync()
    {
        var f = await Fixture.CreateAsync();
        await f.DayAsync(f.Sunday, 8, 0, 14, 0);
        await f.DayAsync(f.Sunday, 10, 0, 12, 0, "Duty");
        await using (var db = f.Factory.CreateDbContext())
        {
            var hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.CurrentWeekHours == 6 && hours.OfficialHours == 6 && hours.DutyHours == 2 && hours.DutyOverlapHours == 2,
                "Official/duty overlap changed the worked total");
            var rule = await db.JobTypeWorkingHourRules.SingleAsync(); rule.CountDutyHours = true; await db.SaveChangesAsync();
            hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.CurrentWeekHours == 6, "Duty inside official hours was counted twice");
        }
        await f.DayAsync(f.Sunday.AddDays(1), 16, 0, 18, 0, "Duty");
        await using (var db = f.Factory.CreateDbContext())
        {
            var hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.CurrentWeekHours == 8, "Distinct duty time did not contribute when configured");
            var rule = await db.JobTypeWorkingHourRules.SingleAsync(); rule.CountDutyHours = false; await db.SaveChangesAsync();
            hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.CurrentWeekHours == 6 && hours.DutyOutsideOfficialHours == 2, "Separated duty affected official required hours");
        }
        var makeup = await Fixture.CreateAsync();
        await makeup.DayAsync(makeup.Sunday, 8, 0, 12, 0, finishHour: 12);
        await makeup.DayAsync(makeup.Sunday.AddDays(1), 8, 0, 16, 0);
        await using (var db = makeup.Factory.CreateDbContext())
        {
            var assignment = await db.WorkAssignments.OrderBy(a => a.WorkPlan.WorkDate).FirstAsync();
            var request = new Request { RequestId = 1, RequestStateId = 4, RequestTypeId = 90 };
            db.Requests.Add(request); db.LateCheckInRequests.Add(new() { RequestId = 1, Request = request, JobId = 1,
                OrganisationId = 10, IndividualId = 100, WorkAssignmentId = assignment.WorkAssignmentId, Kind = "EarlyCheckOut",
                ScheduledStart = makeup.Sunday.AddHours(8), ScheduledEnd = makeup.Sunday.AddHours(14), RequestedEnd = makeup.Sunday.AddHours(12), AppliedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            var hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, makeup.Sunday);
            Check(hours.RequiredWeekHours == 30 && hours.ApprovedEarlyCheckoutShortfallHours == 2 && hours.MakeupCompletedHours == 2 && hours.MakeupRemainingHours == 0,
                "Early checkout was waived instead of made up, or extra official hours were not credited");
        }
        var overnight = await Fixture.CreateAsync();
        await overnight.DayAsync(overnight.Sunday.AddDays(-1), 22, 0, 6, 0, startHour: 22, finishHour: 6, overnight: true);
        await using (var db = overnight.Factory.CreateDbContext())
        {
            var hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, overnight.Sunday);
            Check(hours.OfficialHours == 6, "Hours after midnight at the week boundary were lost or double-counted");
        }
    }

    private static async Task EligibilityAsync()
    {
        var f = await Fixture.CreateAsync();
        await using (var db = f.Factory.CreateDbContext())
        {
            db.Leaves.Add(new Leaf { LeaveId = 1, JobId = 1, FromDate = f.Sunday, ToDate = f.Sunday, LeaveStateId = 4 });
            await db.SaveChangesAsync();
            var hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.RequiredWeekHours == 24 && hours.LeaveCreditHours == 6, "Approved leave did not reduce the weekly target");
            (await db.JobPositions.SingleAsync()).FromDate = f.Sunday.AddDays(2);
            await db.SaveChangesAsync();
            hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.RequiredWeekHours == 18, "Position approval midweek created hours owed before approval");
            (await db.JobWorkTemplateAssignments.SingleAsync()).IsActive = false; await db.SaveChangesAsync();
            hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.RequiredWeekHours == 0, "Missing official schedule created a weekly shortfall");
            var foreign = await WeeklyWorkingHours.LoadAsync(db, 100, 20, f.Sunday);
            Check(!foreign.HasWorkingHoursRule && foreign.CurrentWeekHours == 0, "Weekly results leaked across tenants");
        }
        var rounding = await Fixture.CreateAsync();
        await using (var db = rounding.Factory.CreateDbContext())
        {
            var rule = await db.JobTypeWorkingHourRules.SingleAsync(); rule.WorkingDaysMask = 127;
            var assignment = await db.JobWorkTemplateAssignments.SingleAsync(); assignment.ScheduleMode = "Rotation";
            assignment.RotationTemplateIds = "[1,1,1,0]"; assignment.RotationAnchorDate = rounding.Sunday;
            await db.SaveChangesAsync();
            var hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, rounding.Sunday);
            Check(hours.RequiredWeekHours == 36, "Rotation required hours must follow six actual template days");
        }
    }

    private static async Task ConfigurationAsync()
    {
        var f = await Fixture.CreateAsync();
        var rule = new JobTypeWorkingHourRule { JobTypeId = 2, RequiredWeeklyMinutes = 1200, EffectiveFrom = f.Sunday };
        await Denied(() => f.Settings.SaveRuleAsync(rule), "Staff configured their own required hours without permission");
        f.Permission = WorkingHoursSettingsService.ConfigurePermission;
        await f.Settings.SaveRuleAsync(rule);
        var data = await f.Settings.LoadAsync();
        Check(data.JobTypes.Any(j => j.Id == 2) && data.Rules.Any(r => r.JobTypeId == 2 && r.RequiredWeeklyMinutes == null), "New Job Types cannot be configured");
        await Denied(() => f.Settings.SaveRuleAsync(rule), "Overlapping working-hour periods were accepted");
        var edited = data.Rules.Single(r => r.JobTypeId == 2); var stale = System.Text.Json.JsonSerializer.Deserialize<JobTypeWorkingHourRule>(System.Text.Json.JsonSerializer.Serialize(edited))!;
        edited.RequiredWeeklyMinutes = 1320; await f.Settings.SaveRuleAsync(edited);
        await Denied(() => f.Settings.SaveRuleAsync(stale), "Stale rule overwrote a newer configuration");
        f.Org = 20;
        await Denied(() => f.Settings.SaveRuleAsync(edited), "Foreign-tenant rule was updated");
        f.Org = 10;
        var temporary = new OrganisationWorkingHourRequirement { JobTypeId = 2, FromDate = DateTime.UtcNow.AddHours(5).Date.AddDays(1), ToDate = DateTime.UtcNow.AddHours(5).Date.AddDays(7), RequiredDailyMinutes = 480, Reason = "Extended working week" };
        await f.Settings.SaveRequirementAsync(temporary);
        await Denied(() => f.Settings.SaveRequirementAsync(temporary), "Overlapping temporary daily requirements were accepted");
        var saved = (await f.Settings.LoadAsync()).Requirements.Single();
        var oldVersion = System.Text.Json.JsonSerializer.Deserialize<OrganisationWorkingHourRequirement>(System.Text.Json.JsonSerializer.Serialize(saved))!;
        saved.Reason = "Revised explanation"; await f.Settings.SaveRequirementAsync(saved);
        await Denied(() => f.Settings.SaveRequirementAsync(oldVersion), "Stale temporary hours overwrote a newer configuration");
        f.Org = 20;
        await Denied(() => f.Settings.SaveRequirementAsync(saved), "Temporary requirement crossed tenant boundaries");
        f.Org = 10; f.Permission = "";
        await Denied(() => f.Settings.SaveRequirementAsync(saved), "Staff changed temporary daily hours without permission");
        f.Org = 10; f.Permission = WorkingHoursSettingsService.AmendPermission;
        var today = DateTime.UtcNow.AddHours(5).Date;
        var change = new OrganisationScheduleAmendment { WorkDate = today, OriginalStart = TimeSpan.FromHours(8), RevisedStart = TimeSpan.FromHours(8.5), Reason = "Rain" };
        await f.Settings.SaveAmendmentAsync(change);
        await using (var db = f.Factory.CreateDbContext())
            Check(await db.StaffNotifications.AnyAsync(n => n.OrganisationId == 10 && n.Kind == "Schedule amendment"), "Affected staff did not receive an amendment notification");
        change.WorkDate = today.AddDays(-1);
        await Denied(() => f.Settings.SaveAmendmentAsync(change), "Historical schedule was amended through ordinary HR settings");
    }

    private static async Task ApprovalsAsync()
    {
        var f = await Fixture.CreateAsync();
        await f.SeedWorkflowAsync();
        await using (var db = f.Factory.CreateDbContext())
        {
            var row = await db.WeeklyHourExceptionRequests.Include(r => r.Request).ThenInclude(r => r.RequestState).SingleAsync();
            var subject = new WorkflowRequestSubject(row.Request, row.JobId, row.IndividualId, row.OrganisationId);
            Check(!await f.Processing.CanApprove(db, subject, await WorkflowRequestProcessing.Instance(db, 1), 1, 100), "Applicant can approve their own weekly exception");
            await Denied(() => f.Processing.ExecuteAsync(db, subject, 3, "Approve", "test", 1, 100, () => Task.CompletedTask, () => Task.CompletedTask), "Self-approval was accepted");
            await Denied(() => f.Processing.ExecuteAsync(db, subject, 2, "Approve", "test", 2, 200, () => Task.CompletedTask, () => Task.CompletedTask), "Stale workflow state was accepted");
            var transition = await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(); db.Remove(transition); await db.SaveChangesAsync();
            await Denied(() => f.Processing.ExecuteAsync(db, subject, 3, "Approve", "test", 2, 200, () => Task.CompletedTask, () => Task.CompletedTask), "Approval bypassed the allowed-transition table");
        }
        // A fresh fixture avoids relying on transaction rollback semantics of the in-memory provider.
        f = await Fixture.CreateAsync(); await f.SeedWorkflowAsync();
        f.Actor = 2;
        var outcome = await f.Exceptions.ActAsync(1, 3, "Approve", "Approved shortfall exception");
        Check(outcome.Success, outcome.Message);
        await using (var db = f.Factory.CreateDbContext())
        {
            var hours = await WeeklyWorkingHours.LoadAsync(db, 100, 10, f.Sunday);
            Check(hours.RequiredWeekHours == 28 && hours.ExceptionCreditHours == 2 && hours.PotentialOvertimeHours == 0,
                "Approved exception did not credit the shortfall or manufactured overtime");
            Check(await db.RequestNotifications.AnyAsync(n => n.UserId == 1 && n.ToStateId == 4), "Approval outcome notification missing");
        }
        f.Actor = 1; f.Org = 20;
        await Denied(() => new WeeklyWorkingHoursService(f.Factory, f.Access).LoadAsync(100, f.Sunday), "Foreign-tenant weekly review was allowed");
    }

    private sealed class Fixture
    {
        public readonly DateTime Sunday = new(2026, 1, 4);
        public ReceiptFactory Factory = null!;
        public int Actor = 1, Org = 10;
        public string Permission = "";
        public IUserAccessService Access = null!;
        public WorkingHoursSettingsService Settings = null!;
        public AttendanceService Attendance = null!;
        public WorkflowRequestProcessing Processing = null!;
        public WeeklyHourExceptionService Exceptions = null!;
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            f.Factory = new(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
            f.Access = Proxy.For<IUserAccessService>((m, a) => m switch {
                "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = f.Actor, IndividualId = f.Actor * 100, ActiveJob = new() { JobId = f.Actor, OrganisationId = f.Org } }),
                "GetCurrentOrganisationIdAsync" => Task.FromResult(f.Org), "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == f.Org),
                "HasPermissionAsync" => Task.FromResult((string)a[0]! == f.Permission), _ => throw new Exception(m) });
            var logs = Proxy.For<IOperationLogService>((m, a) => m == "CreateAsync" ? LogAsync((HrmTeContext)a[0]!) : throw new Exception(m));
            f.Settings = new(f.Factory, f.Access, logs);
            f.Attendance = new(f.Factory, null!, null!, f.Access, null!, NullLogger<AttendanceService>.Instance);
            var resolver = Proxy.For<IWorkflowApproverResolver>((_, _) => Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>([new(2, 1, 1, true)]));
            var contexts = new WorkflowRequestContextResolver(f.Factory);
            f.Processing = new(resolver, contexts);
            f.Exceptions = new(f.Factory, f.Access, null!, logs, null!, resolver, contexts);
            await using var db = f.Factory.CreateDbContext();
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant A" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Tenant B" });
            db.UserOrganisations.Add(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 });
            db.Users.AddRange(new User { UserId = 1, BusinessEntityID = 100, UserOrganisationId = 1, Username = "employee", IsActive = true, LocalUserStateId = 1 },
                new User { UserId = 2, BusinessEntityID = 200, UserOrganisationId = 1, Username = "supervisor", IsActive = true, LocalUserStateId = 1 });
            db.Individuals.AddRange(new Individual { BusinessEntityId = 100, FirstNameEnglish = "Employee" }, new Individual { BusinessEntityId = 200, FirstNameEnglish = "Supervisor" });
            db.JobTypes.AddRange(new JobType { JobTypeId = 1, TypeName = "Contract" }, new JobType { JobTypeId = 2, TypeName = "Internship" });
            db.Jobs.Add(new Job { JobId = 1, IndividualID = 100, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = f.Sunday.AddYears(-1) });
            db.JobPositions.Add(new JobPosition { JobPositionId = 1, JobId = 1, PositionId = 1, JobPositionStateId = 4, FromDate = f.Sunday.AddYears(-1) });
            db.JobTypeWorkingHourRules.Add(new() { OrganisationId = 10, JobTypeId = 1, RequiredWeeklyMinutes = 1800, EffectiveFrom = f.Sunday.AddDays(-7), WorkingDaysMask = 31 });
            db.WorkTemplateTypes.Add(new WorkTemplateType { WorkTemplateTypeId = 1, Code = "OFFICIAL", Name = "Official", IsActive = true });
            foreach (var (id, code) in new[] { (1, "CHECK_IN"), (2, "WORK"), (3, "CHECK_OUT") }) db.WorkSegmentTypes.Add(new() { WorkSegmentTypeId = id, Code = code, Name = code, IsActive = true });
            db.WorkAssignmentStates.Add(new() { WorkAssignmentStateId = 1, Code = "PLANNED", Name = "Planned", IsActive = true });
            db.WorkTemplates.Add(new WorkTemplate { WorkTemplateId = 1, WorkTemplateTypeId = 1, OrganisationBusinessEntityId = 10,
                Name = "Official", IsActive = true, RequiresAttendance = true, RequiresCheckOut = true, DefaultStartTime = new(8, 0), DefaultEndTime = new(14, 0) });
            db.JobWorkTemplateAssignments.Add(new() { JobWorkTemplateAssignmentID = 1, JobID = 1, WorkTemplateID = 1, IsActive = true,
                EffectiveFrom = f.Sunday.AddYears(-1), WorkingDaysMask = 31 });
            await db.SaveChangesAsync(); return f;
        }
        static async Task<OperationLog> LogAsync(HrmTeContext db) { var log = new OperationLog(); db.Add(log); await db.SaveChangesAsync(); return log; }
        public async Task DayAsync(DateTime date, int inHour, int inMinute, int outHour, int outMinute, string purpose = "Official", int startHour = 8, int finishHour = 14, bool overnight = false)
        {
            await using var db = Factory.CreateDbContext();
            var plan = await db.WorkPlans.SingleOrDefaultAsync(p => p.WorkDate == date && p.IndividualId == 100);
            if (plan == null) { plan = new WorkPlan { JobId = 1, IndividualId = 100, OrganisationBusinessEntityId = 10, WorkDate = date, IsValid = true }; db.Add(plan); }
            var start = date.AddHours(startHour); var end = date.AddDays(overnight ? 1 : 0).AddHours(finishHour);
            var assignment = new WorkAssignment { WorkPlan = plan, WorkTemplateId = 1, WorkTemplateTypeId = 1, WorkAssignmentStateId = 1,
                Name = purpose, SchedulePurpose = purpose, IsValid = true, RequiresAttendance = true, RequiresCheckOut = true, StartDateTime = start, EndDateTime = end };
            db.Add(assignment);
            foreach (var (type, at, minutes) in new[] { (1, start, 60), (2, start, (int)(end-start).TotalMinutes), (3, end, 180) })
            {
                var segment = new WorkAssignmentSegment { WorkAssignment = assignment, WorkSegmentTypeId = type, SequenceNumber = type,
                    WorkTemplateSegmentId = type, StartDateTime = at, EndDateTime = at.AddMinutes(minutes), Name = purpose, IsValid = true, IsMandatory = type != 2, RequiresAttendance = type != 2 };
                db.Add(segment);
                db.WorkPlanSegments.Add(new() { WorkPlan = plan, WorkTemplateSegmentId = type, WorkSegmentTypeId = type, SchedulePurpose = purpose,
                    SequenceNumber = type, StartDateTime = at, EndDateTime = at.AddMinutes(minutes), Name = purpose, IsValid = true, IsMandatory = type != 2, RequiresAttendance = type != 2 });
            }
            await db.SaveChangesAsync();
            foreach (var (type, timestamp) in new[] { (1, date.AddHours(inHour).AddMinutes(inMinute)), (2, date.AddDays(overnight ? 1 : 0).AddHours(outHour).AddMinutes(outMinute)) })
            {
                var segmentType = type == 1 ? 1 : 3;
                var boundary = await db.WorkPlanSegments.OrderByDescending(s => s.WorkPlanSegmentId).FirstAsync(s => s.WorkPlanId == plan.WorkPlanId && s.SchedulePurpose == purpose && s.WorkSegmentTypeId == segmentType);
                var log = new AttendanceLog { IndividualId = 100, OrganisationId = 10, Date = timestamp, AttendanceLogStateId = 1, InOutModeId = type };
                db.Add(log); db.AttendanceLogResolutions.Add(new() { AttendanceLog = log, WorkPlan = plan, WorkAssignment = assignment,
                    WorkPlanSegmentId = boundary.WorkPlanSegmentId, AttendanceClockTypeId = type, AttendanceResolutionStatusId = HRM.Constants.AttendanceResolutionStatusIds.Resolved, IsValid = true });
            }
            await db.SaveChangesAsync();
        }
        public async Task SeedWorkflowAsync()
        {
            await using var db = Factory.CreateDbContext();
            db.RequestStates.AddRange(new RequestState { RequestStateId = 3, StateName = "Pending Supervisor Approval", IsProcessingState = true },
                new RequestState { RequestStateId = 4, StateName = "Approved", IsFinalState = true });
            db.RequestTypes.Add(new RequestType { RequestTypeId = 90, TypeName = WeeklyHourExceptionService.TypeName, ViewUrl = WeeklyHourExceptionService.PageUrl });
            db.RequestTypesAllowedRequestStateTransitions.Add(new() { RequestTypeId = 90, FromRequestStateId = 3, ToRequestStateId = 4 });
            var definition = new WfWorkflowDefinition { WorkflowDefinitionId = 1, OrganisationBusinessEntityId = 10, RequestTypeId = 90, IsActive = true, Name = "Supervisor",
                WorkflowJobTypes = [new() { JobTypeId = 1, IsActive = true }], Stages = [new() { WorkflowStageId = 1, StageOrder = 1, StageName = "Supervisor", RequestStateId = 3, ApprovalMode = "Any" }] };
            db.Add(definition);
            var request = new Request { RequestId = 1, RequestTypeId = 90, OrganisationBusinessEntityID = 10, ApplicantBusinessEntityId = 100, RequestStateId = 3, ReferenceNumber = "TEST-WHE-1" };
            db.Add(request); db.WeeklyHourExceptionRequests.Add(new() { Request = request, RequestId = 1, OrganisationId = 10, JobId = 1, IndividualId = 100,
                WeekStart = Sunday, ExcusedMinutes = 120, Reason = "Approved medical appointment" });
            var instance = new WfRequestInstance { WorkflowDefinition = definition, RequestId = 1, WorkflowStatus = "InProgress" };
            db.Add(instance);
            var stage = new WfRequestStageInstance { WorkflowRequestInstance = instance, WorkflowStage = definition.Stages.Single(), StageStatus = "Pending",
                Approvals = [new() { ApproverUserId = 2, StageApproverId = 1, Decision = "Pending", ApprovalSequence = 1 }] };
            db.Add(stage); await db.SaveChangesAsync(); instance.CurrentRequestStageInstanceId = stage.RequestStageInstanceId; await db.SaveChangesAsync();
        }
    }
}
