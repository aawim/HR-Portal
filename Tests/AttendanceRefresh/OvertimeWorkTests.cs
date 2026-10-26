using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class OvertimeWorkTests
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static readonly DateTime Today = DateTime.UtcNow.AddHours(5).Date;
    public static async Task RunAsync()
    {
        var f = new Fixture(); await f.Seed();
        Check(!(await f.Service.SubmitAsync(Today, 60, "Typed hours")).Success, "Arbitrary hours accepted");
        Check(!(await f.Service.SubmitClaimAsync(1, 8, "Foreign end")).Success, "Foreign clock accepted");
        Check(!(await f.Service.SubmitClaimAsync(3, 2, "Official hours")).Success, "Official overlap accepted");
        Check(!(await f.Service.SubmitClaimAsync(2, 1, "Reversed")).Success, "Reversed events accepted");
        Check(!(await f.Service.SubmitClaimAsync(1, 1, "Same event")).Success, "Same event accepted");
        Check(!(await f.Service.SubmitClaimAsync(1, 2, "Unapproved task", 500)).Success, "Nonexistent task accepted");
        var options = await f.Service.LoadClockEventsAsync(Today.AddDays(-1));
        Check(options.Any(x => x.Id == 1) && !options.Any(x => x.Id == 3 || x.Id == 8), "Clock selector exposed official/foreign events");
        Check((await f.Service.SubmitClaimAsync(1, 2, "Unplanned extra work")).Success, "Unplanned claim failed");
        Check(!(await f.Service.SubmitClaimAsync(1, 2, "Repeat")).Success, "Double claim accepted");
        Check(!(await f.Service.SubmitClaimAsync(2, 5, "Reuse previous end as a start")).Success, "Used boundary clock was reused");
        var remaining = await f.Service.LoadClockEventsAsync(Today.AddDays(-1));
        Check(!remaining.Any(x => x.Id == 1 || x.Id == 2) && remaining.Any(x => x.Id == 4), "Used events remained selectable or unused events disappeared");
        int claim;
        await using (var db = f.Factory.CreateDbContext())
        {
            var row = await db.OvertimeRequests.SingleAsync(); claim = row.RequestId;
            Check(row.Minutes == 60 && row.StartLogId == 1 && row.EndLogId == 2 && row.ClaimSource == "Unplanned", "DB evidence not saved");
        }
        var denied = false; try { await f.Service.ActAsync(claim, 2, "Approve", "Self"); } catch (UnauthorizedAccessException) { denied = true; }
        Check(denied, "Self approval accepted");
        f.AsReviewer(); Check((await f.Service.ActAsync(claim, 2, "Approve", "Verified")).Success, "Claim approval failed");
        f.AsStaff();
        Check(!(await f.Service.LoadClockEventsAsync(Today.AddDays(-1))).Any(x => x.Id == 1 || x.Id == 2), "Approved claim events became selectable again");
        Check(!(await f.Service.SubmitWorkAsync("Preapproval", Today, Today.AddDays(1), "Planned", [])).Success, "Empty task list accepted");
        Check((await f.Service.SubmitWorkAsync("Preapproval", Today, Today.AddDays(1), "Planned", ["Deploy release", "Verify backup"])).Success, "Preapproval submit failed");
        int preapproval; int task;
        await using (var db = f.Factory.CreateDbContext()) { var w = await db.OvertimeWorkRequests.Include(x => x.Tasks).SingleAsync(); preapproval = w.RequestId; task = w.Tasks.First().Id; }
        Check(!(await f.Service.LoadApprovedTasksAsync()).Any(), "Pending preapproval exposed tasks");
        Check(!(await f.Service.SubmitClaimAsync(4, 5, "Pending task", task)).Success, "Pending preapproval accepted as claim authority");
        f.AsReviewer(); Check((await f.Service.ActWorkAsync(preapproval, 2, "Approve", "Proceed")).Success, "Preapproval could not be approved");
        f.AsStaff(); Check((await f.Service.LoadApprovedTasksAsync()).Count == 2, "Approved tasks missing");
        Check(!(await f.Service.SubmitClaimAsync(4, 5, "Before approved date", task)).Success, "Task work outside approved range accepted");
        // Use a completed interval today, without depending on the time of day at test execution.
        await using (var db = f.Factory.CreateDbContext())
        {
            var pre = await db.OvertimeWorkRequests.SingleAsync(); pre.FromDate = Today.AddDays(-1);
            await db.SaveChangesAsync();
        }
        Check((await f.Service.SubmitClaimAsync(4, 5, "Approved task completed", task)).Success, "Planned task claim failed");
        var a = Today.AddDays(-1).AddHours(17); var b = a.AddHours(1);
        Check((await f.Service.SubmitWorkAsync("ClockEvents", a, b, "Missing OT punches", [], a, b)).Success, "Missing clock request failed");
        int clockRequest;
        await using (var db = f.Factory.CreateDbContext())
        {
            clockRequest = await db.OvertimeWorkRequests.Where(x => x.Kind == "ClockEvents").Select(x => x.RequestId).SingleAsync();
            Check(!await db.AttendanceLogs.AnyAsync(l => l.Date == a), "Unapproved clock proposal changed attendance");
        }
        f.AsReviewer();
        Check((await f.Service.ActWorkAsync(clockRequest, 2, "Approve", "Verified missing punches")).Success, "Clock approval failed");
        Check(!(await f.Service.ActWorkAsync(clockRequest, 2, "Approve", "Replay")).Success, "Clock approval replay accepted");
        await using (var db = f.Factory.CreateDbContext())
        {
            var w = await db.OvertimeWorkRequests.SingleAsync(x => x.RequestId == clockRequest);
            Check(w.StartLogId.HasValue && w.EndLogId.HasValue && await db.AttendanceLogs.CountAsync(l => l.Date == a || l.Date == b) == 2, "Approved clocks not created exactly once");
        }
        f.AsStaff();
        Check(!(await f.Service.SubmitWorkAsync("ClockEvents", a, b, "Duplicate", [], a, b)).Success, "Duplicate clock request accepted");
        // Recheck policy and evidence at approval, not just submission.
        int pending; await using (var db = f.Factory.CreateDbContext()) pending = await db.OvertimeRequests.Where(x => x.RequestId != claim).Select(x => x.RequestId).SingleAsync();
        await using (var db = f.Factory.CreateDbContext()) { (await db.JobTypeOvertimeRules.SingleAsync()).IsAllowed = false; await db.SaveChangesAsync(); }
        f.AsReviewer(); Check(!(await f.Service.ActAsync(pending, 2, "Approve", "Policy disabled")).Success, "Approval ignored disabled Job Type OT");
        f.AsStaff(); Check(!(await f.Service.SubmitWorkAsync("Preapproval", Today, Today, "Disabled", ["Task"])).Success, "Disabled Job Type preapproval allowed");
        await using (var db = f.Factory.CreateDbContext()) { (await db.JobTypeOvertimeRules.SingleAsync()).IsAllowed = true; await db.SaveChangesAsync(); }
        // Cap and timestamp tampering are independently rejected.
        await using (var db = f.Factory.CreateDbContext()) { (await db.JobTypeOvertimeRules.SingleAsync()).MaximumWeeklyHours = 1; await db.SaveChangesAsync(); }
        Check(!(await f.Service.SubmitClaimAsync(6, 7, "Over weekly allowance")).Success, "Weekly cap ignored pending/approved work");
        await using (var db = f.Factory.CreateDbContext()) { (await db.AttendanceLogs.SingleAsync(x => x.AttendanceLogId == 4)).Date = Today.AddDays(-1).AddHours(16).AddMinutes(1); await db.SaveChangesAsync(); }
        f.AsReviewer(); Check(!(await f.Service.ActAsync(pending, 2, "Approve", "Clock changed")).Success, "Changed clock snapshot accepted");
        // Rejection/cancellation never creates the proposed punches, and allowed transitions are mandatory.
        var g = new Fixture(); await g.Seed();
        var first = Today.AddDays(-1).AddHours(17); var last = first.AddHours(1);
        Check((await g.Service.SubmitWorkAsync("ClockEvents", first, last, "Missing", [], first, last)).Success, "Clock proposal failed");
        int correction;
        await using (var db = g.Factory.CreateDbContext()) correction = await db.OvertimeWorkRequests.Select(x => x.RequestId).SingleAsync();
        g.AsReviewer();
        await using (var db = g.Factory.CreateDbContext())
        {
            db.RequestTypesAllowedRequestStateTransitions.Remove(await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(x => x.RequestTypeId == 102 && x.ToRequestStateId == 4));
            await db.SaveChangesAsync();
        }
        Check(!(await g.Service.ActWorkAsync(correction, 2, "Approve", "Blocked transition")).Success, "Missing transition allowed clock approval");
        await using (var db = g.Factory.CreateDbContext()) Check(!await db.AttendanceLogs.AnyAsync(x => x.Date == first), "Blocked approval created a punch");
        Check((await g.Service.ActWorkAsync(correction, 2, "Reject", "Not supported")).Success, "Clock rejection failed");
        await using (var db = g.Factory.CreateDbContext()) Check(!await db.AttendanceLogs.AnyAsync(x => x.Date == first), "Rejected clock proposal created a punch");
        g.AsStaff();
        Check((await g.Service.SubmitClaimAsync(1, 2, "Pending work")).Success, "New claim failed");
        int cancelled;
        await using (var db = g.Factory.CreateDbContext()) cancelled = await db.OvertimeRequests.Select(x => x.RequestId).SingleAsync();
        Check((await g.Service.ActAsync(cancelled, 2, "Cancel", "Withdrawn")).Success, "Own claim cancellation failed");
        Check((await g.Service.LoadClockEventsAsync(Today.AddDays(-1))).Any(x => x.Id == 1) &&
            (await g.Service.LoadClockEventsAsync(Today.AddDays(-1))).Any(x => x.Id == 2), "Cancelled claim did not restore selectable events");
        Check((await g.Service.SubmitClaimAsync(1, 2, "Resubmitted after cancellation")).Success, "Cancelled claim did not release interval");
        // An overnight interval belongs to two daily/weekly buckets, not just its start date.
        var night = new Fixture(); await night.Seed();
        var sunday = Today.AddDays(-(int)Today.DayOfWeek - 7);
        var saturday = sunday.AddDays(-1);
        await using (var db = night.Factory.CreateDbContext())
        {
            var policy = await db.JobTypeOvertimeRules.SingleAsync(); policy.MaximumWeeklyHours = 1; policy.MaximumDailyHours = 1;
            for (var i=0;i<2;i++)
            {
                var day = saturday.AddDays(i);
                db.WorkPlans.Add(new() { WorkPlanId=20+i, JobId=1, IndividualId=200, OrganisationBusinessEntityId=10, WorkDate=day, IsValid=true });
                db.WorkAssignments.Add(new() { WorkAssignmentId=20+i, WorkPlanId=20+i, WorkTemplateTypeId=1, IsValid=true, SchedulePurpose="Official", StartDateTime=day.AddHours(8), EndDateTime=day.AddHours(14) });
            }
            var times = new[] { saturday.AddHours(23), sunday.AddHours(1), sunday.AddHours(2), sunday.AddHours(3) };
            for (var i=0;i<times.Length;i++) db.AttendanceLogs.Add(new() { AttendanceLogId=20+i, IndividualId=200, OrganisationId=10, AttendanceLogStateId=1, Date=times[i] });
            await db.SaveChangesAsync();
        }
        Check((await night.Service.SubmitClaimAsync(20,21,"Overnight across weeks")).Success, "Overnight claim was not split across daily/weekly limits");
        Check(!(await night.Service.SubmitClaimAsync(22,23,"Next interval over cap")).Success, "Overnight claim failed to reserve the next day/week allowance");
        f.Org = 20;
        denied = false; try { await f.Service.ActAsync(claim, 4, "Approve", "Other tenant"); } catch (UnauthorizedAccessException) { denied = true; }
        Check(denied, "Cross-tenant claim accessible");
    }
    sealed class Fixture
    {
        public ReceiptFactory Factory { get; } = new(new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        public int Org = 10;
        UserContextDto actor = new() { UserId = 2, IndividualId = 200 };
        public OvertimeRequestService Service { get; }
        public void AsReviewer() => actor = new() { UserId = 1, IndividualId = 100 };
        public void AsStaff() => actor = new() { UserId = 2, IndividualId = 200 };
        public Fixture()
        {
            var access = Proxy.For<IUserAccessService>((m,a) => m switch {
                "RequireContextAsync" => Task.FromResult(actor), "GetCurrentOrganisationIdAsync" => Task.FromResult(Org),
                "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == Org), "HasPermissionAsync" => Task.FromResult(actor.UserId == 1), _ => throw new Exception(m) });
            var logs = Proxy.For<IOperationLogService>((m,a) => m == "CreateAsync" ? Audit((HrmTeContext)a[0]!) : throw new Exception(m));
            var requests = Proxy.For<IRequestService>((m,a) => m == "CreateAsync" ? Create((HrmTeContext)a[0]!, (CreateRequestDto)a[1]!) : throw new Exception(m));
            var engine = Proxy.For<IWorkflowEngine>((m,a) => m == "StartAsync" ? Start((HrmTeContext)a[0]!, Convert.ToInt32(a[1])) : throw new Exception(m));
            var resolver = Proxy.For<IWorkflowApproverResolver>((m,a) => Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>([new(1, 1, 1, true)]));
            Service = new(Factory, access, requests, logs, engine, resolver, new WorkflowRequestContextResolver(Factory));
        }
        static async Task<OperationLog> Audit(HrmTeContext db) { var a = new OperationLog(); db.OperationLogs.Add(a); await db.SaveChangesAsync(); return a; }
        static async Task<Request> Create(HrmTeContext db, CreateRequestDto dto)
        {
            var r = new Request { RequestTypeId = dto.RequestTypeId, RequestStateId = 2, ApplicantBusinessEntityId = dto.ApplicantBusinessEntityId,
                OrganisationBusinessEntityID = dto.OrganisationBusinessEntityId, RequestEffectiveDate = dto.EffectiveDate, ApplicationDate = DateTime.UtcNow };
            db.Requests.Add(r); await db.SaveChangesAsync(); return r;
        }
        static async Task<long> Start(HrmTeContext db, int id)
        {
            var request = await db.Requests.SingleAsync(x => x.RequestId == id);
            var def = await db.WfWorkflowDefinitions.Include(x => x.Stages).SingleAsync(x => x.RequestTypeId == request.RequestTypeId);
            var instance = new WfRequestInstance { RequestId = id, WorkflowDefinitionId = def.WorkflowDefinitionId };
            db.WfRequestInstances.Add(instance); await db.SaveChangesAsync();
            var stage = new WfRequestStageInstance { WorkflowRequestInstanceId = instance.WorkflowRequestInstanceId,
                WorkflowStageId = def.Stages.Single().WorkflowStageId, Approvals = [new() { ApproverUserId = 1, Decision = "Pending" }] };
            db.WfRequestStageInstances.Add(stage); await db.SaveChangesAsync(); instance.CurrentRequestStageInstanceId = stage.RequestStageInstanceId;
            await db.SaveChangesAsync(); return instance.WorkflowRequestInstanceId;
        }
        public async Task Seed()
        {
            await using var db = Factory.CreateDbContext();
            db.Organisations.Add(new() { BusinessEntityID = 10, OrganisationName = "Tenant" });
            db.UserOrganisations.Add(new() { UserOrganisationID = 1, BusinessEntityID = 10 });
            db.Users.AddRange(new User { UserId = 1, BusinessEntityID = 100, UserOrganisationId = 1, Username = "hr", IsActive = true, LocalUserStateId = 1 },
                new User { UserId = 2, BusinessEntityID = 200, UserOrganisationId = 1, Username = "staff", IsActive = true, LocalUserStateId = 1 });
            db.Individuals.AddRange(new Individual { BusinessEntityId = 100, FirstNameEnglish = "HR" }, new Individual { BusinessEntityId = 200, FirstNameEnglish = "Staff" });
            db.JobTypes.Add(new() { JobTypeId = 1, TypeName = "Contract" });
            db.Jobs.Add(new() { JobId = 1, IndividualID = 200, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = Today.AddYears(-1) });
            db.JobTypeOvertimeRules.Add(new() { OrganisationId = 10, JobTypeId = 1, IsAllowed = true, MaximumDailyHours = 4, MaximumWeeklyHours = 6 });
            db.RequestStates.AddRange(new RequestState { RequestStateId = 2, StateName = "Pending", IsProcessingState = true },
                new RequestState { RequestStateId = 4, StateName = "Approved", IsFinalState = true },
                new RequestState { RequestStateId = 5, StateName = "Cancelled", IsFinalState = true }, new RequestState { RequestStateId = 6, StateName = "Rejected", IsFinalState = true });
            var names = new[] { OvertimeRequestService.TypeName, OvertimeRequestService.PreapprovalTypeName, OvertimeRequestService.ClockTypeName };
            for (var i = 0; i < names.Length; i++)
            {
                var id = 100 + i;
                db.RequestTypes.Add(new() { RequestTypeId = id, TypeName = names[i] });
                foreach (var state in new[] { 4, 5, 6 }) db.RequestTypesAllowedRequestStateTransitions.Add(new() { RequestTypeId = id, FromRequestStateId = 2, ToRequestStateId = state });
                db.WfWorkflowDefinitions.Add(new() { WorkflowDefinitionId = id, RequestTypeId = id, OrganisationBusinessEntityId = 10, Name = names[i],
                    Stages = [new() { WorkflowStageId = id, StageOrder = 1, RequestStateId = 2, StageName = "HR", ApprovalMode = "Any" }] });
            }
            db.WorkTemplateTypes.Add(new() { WorkTemplateTypeId = 1, Code = "OFFICIAL", Name = "Official" });
            db.WorkPlans.Add(new() { WorkPlanId = 1, JobId = 1, IndividualId = 200, OrganisationBusinessEntityId = 10, WorkDate = Today.AddDays(-1), IsValid = true });
            db.WorkAssignments.Add(new() { WorkAssignmentId = 1, WorkPlanId = 1, WorkTemplateTypeId = 1, IsValid = true,
                SchedulePurpose = "Official", StartDateTime = Today.AddDays(-1).AddHours(8), EndDateTime = Today.AddDays(-1).AddHours(14) });
            var hours = new[] { 14, 15, 10, 15, 16, 18, 19, 20 };
            for (var i = 0; i < hours.Length; i++) db.AttendanceLogs.Add(new() { AttendanceLogId = i + 1, OrganisationId = i == 7 ? 20 : 10,
                IndividualId = i == 7 ? 300 : 200, AttendanceLogStateId = 1, Date = Today.AddDays(-1).AddHours(hours[i]).AddMinutes(i == 3 ? 1 : 0) });
            await db.SaveChangesAsync();
        }
    }
}
