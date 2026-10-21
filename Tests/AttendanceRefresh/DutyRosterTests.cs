using HRM.DTOs.UserContext;
using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.WorkPlanning;
using HRM.Models.Workflow;
using HRM.Models.LeaveTypes;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Results;
using HRM.WorkPlanning.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

static class DutyRosterTests
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var consent = await Fixture.CreateAsync();
        Check((await consent.Service.SubmitAsync(new(1, 2, null, "Both staff consent"))).Success, "Consent submission failed");
        var consentRow = (await consent.Service.GetRequestsAsync()).Single();
        Check(consentRow.CanAccept && !consentRow.CanApprove && consentRow.SourceAcceptance == "Pending", "Submitting was incorrectly counted as source acceptance");
        consent.Actor = 2; Check((await consent.Service.ActAsync(consentRow.Id, 10, "Accept")).Success, "Target-first acceptance failed");
        consentRow = (await consent.Service.GetRequestsAsync()).Single();
        Check(consentRow.StateId == 10 && consentRow.SourceAcceptance == "Pending" && consentRow.TargetAcceptance == "Accepted", "Target alone advanced to supervisor approval");
        Check(!(await consent.Service.ActAsync(consentRow.Id, 10, "Accept")).Success, "One staff member accepted twice");
        consent.Actor = 3; Check(!(await consent.Service.ActAsync(consentRow.Id, 10, "Approve")).Success, "Supervisor bypassed source acceptance");
        consent.Actor = 1; Check((await consent.Service.ActAsync(consentRow.Id, 10, "Accept")).Success, "Source second acceptance failed");
        consentRow = (await consent.Service.GetRequestsAsync()).Single(); Check(consentRow.StateId == 3 && !consentRow.CanApprove && consentRow.SourceAcceptance == "Accepted" && consentRow.TargetAcceptance == "Accepted", "Both acceptance records were not required");
        var f = await Fixture.CreateAsync();
        var result = await f.SubmitAsync(new(1, 2, null, "Cover my duty")); Check(result.Success, result.Message);
        Check(!(await f.SubmitAsync(new(1, 2, null, "Duplicate"))).Success, "Duplicate duty reservation accepted");
        var row = (await f.Service.GetRequestsAsync()).Single(); Check(row.CanCancel && !row.CanAccept && !row.CanApprove, "Source can accept/approve own request");
        Check(!(await f.Service.ActAsync(row.Id, 10, "Accept")).Success, "Source accepted own transfer");
        Check(!(await f.Service.ActAsync(row.Id, 10, "Approve")).Success, "Approval bypassed staff acceptance");
        f.Actor = 2; row = (await f.Service.GetRequestsAsync()).Single(); Check(row.CanAccept, "Target cannot accept");
        var auth = new ContextAuthentication(); auth.SignIn("staff2");
        var alerts = await new RequestNotificationService(f.Factory, auth).GetInboxAsync();
        Check(alerts.Items.Any(x => x.Url == "/online/duty-roster" && x.Message!.Contains("acceptance")), "Acceptance bell alert has wrong destination");
        Check((await f.Service.ActAsync(row.Id, 10, "Accept")).Success, "Acceptance failed");
        Check(!(await f.Service.ActAsync(row.Id, 3, "Approve")).Success, "Target approved participating request");
        f.Actor = 3; row = (await f.Service.GetRequestsAsync()).Single(); Check(row.CanApprove, "Supervisor cannot approve");
        auth.SignIn("staff3"); alerts = await new RequestNotificationService(f.Factory, auth).GetInboxAsync();
        Check(alerts.Items.Any(x => x.Url == "/online/duty-roster" && x.ActionStage == "Supervisor approval"), "Supervisor bell alert missing or has wrong destination");
        f.Eligible = false; Check(!(await f.Service.ActAsync(row.Id, 3, "Approve")).Success, "Revoked approver retained access"); f.Eligible = true;
        Check(!(await f.Service.ActAsync(row.Id, 10, "Approve")).Success, "Stale state accepted");
        result = await f.Service.ActAsync(row.Id, 3, "Approve"); Check(result.Success, result.Message);
        Check(!(await f.Service.ActAsync(row.Id, 3, "Approve")).Success, "Final request was approved twice");
        await using (var db = f.Factory.CreateDbContext())
        {
            var request = await db.DutyChangeRequests.SingleAsync(); Check(!request.IsOpen && request.TargetNewAssignmentId.HasValue, "Transfer did not close atomically");
            var moved = await db.WorkAssignments.Include(x => x.WorkPlan).Include(x => x.WorkAssignmentSegments).SingleAsync(x => x.WorkAssignmentId == request.TargetNewAssignmentId);
            Check(moved.WorkPlan.JobId == 2 && moved.WorkPlan.IndividualId == 200, "Incoming duty has wrong staff plan");
            Check(moved.StartDateTime == f.Day.AddHours(10) && moved.WorkAssignmentSegments.Count == 2, "Transfer lost dated times/segments");
            Check(!(await db.WorkAssignments.FindAsync(1L))!.IsValid, "Source duty remains active");
            Check((await db.WorkAssignments.FindAsync(2L))!.IsValid, "Official duty was modified");
            Check(await db.RequestNotifications.AnyAsync(x => x.UserId == 2 && x.NotificationKind == "DutyAcceptance"), "Target acceptance notification missing");
            Check(await db.RequestNotifications.AnyAsync(x => x.UserId == 2 && x.ToStateId == 4), "Target outcome notification missing");
            Check(await db.WfRequestApprovals.AnyAsync(x => x.ApproverUserId == 3 && x.Decision == "Approved"), "Supervisor decision not recorded");
        }
        var provider = Proxy.For<IPlanningProviderResolver>((_, _) => Task.FromResult(new PlanningProviderResolution { PlanningProviderId = 1, Code = "WORK_PLANNING" }));
        await new WorkPlanGenerator(f.Factory, provider).GenerateOrGetAsync(100, 1, 10, DateOnly.FromDateTime(f.Day));
        await using (var db = f.Factory.CreateDbContext()) Check(!await db.WorkAssignments.AnyAsync(x => x.WorkPlan.JobId == 1 && x.IsValid && x.SchedulePurpose == "Duty"), "Recurring generator recreated transferred duty");

        var swap = await Fixture.CreateAsync();
        await using (var db = swap.Factory.CreateDbContext()) { db.WorkAssignments.Add(Assignment(3, 2, 200, swap.Day.AddDays(1), "Duty")); await db.SaveChangesAsync(); }
        result = await swap.SubmitAsync(new(1, 2, 3, "Exchange dates")); Check(result.Success, result.Message);
        swap.Actor = 2; row = (await swap.Service.GetRequestsAsync()).Single(); Check((await swap.Service.ActAsync(row.Id, 10, "Accept")).Success, "Swap acceptance failed");
        swap.Actor = 3; result = await swap.Service.ActAsync(row.Id, 3, "Approve"); Check(result.Success, result.Message);
        await using (var db = swap.Factory.CreateDbContext())
        { var c = await db.DutyChangeRequests.SingleAsync(); Check(c.SourceNewAssignmentId.HasValue && c.TargetNewAssignmentId.HasValue && !c.IsOpen, "Swap did not apply both assignments");
          Check(await db.WorkAssignments.CountAsync(x => x.IsValid && x.SchedulePurpose == "Duty") == 2, "Swap duplicated or lost duties"); }

        var conflict = await Fixture.CreateAsync();
        await using (var db = conflict.Factory.CreateDbContext()) { db.WorkAssignments.Add(Assignment(3, 2, 200, conflict.Day, "Duty")); await db.SaveChangesAsync(); }
        Check(!(await conflict.SubmitAsync(new(1, 2, null, "Overlapping duty"))).Success, "Overlapping duty accepted");
        var leave = await Fixture.CreateAsync();
        await using (var db = leave.Factory.CreateDbContext()) { db.Leaves.Add(new Leaf { JobId = 2, LeaveStateId = 4, FromDate = leave.Day, ToDate = leave.Day }); await db.SaveChangesAsync(); }
        Check(!(await leave.SubmitAsync(new(1, 2, null, "On leave"))).Success, "Approved leave conflict accepted");
        var tenant = await Fixture.CreateAsync();
        await using (var db = tenant.Factory.CreateDbContext()) { (await db.Jobs.FindAsync(2))!.OrganisationID = 20; await db.SaveChangesAsync(); }
        Check(!(await tenant.SubmitAsync(new(1, 2, null, "Foreign tenant"))).Success, "Cross-tenant transfer accepted");
        var historical = await Fixture.CreateAsync();
        await using (var db = historical.Factory.CreateDbContext()) { (await db.WorkAssignments.FindAsync(1L))!.StartDateTime = DateTime.UtcNow.AddDays(-1); await db.SaveChangesAsync(); }
        Check(!(await historical.SubmitAsync(new(1, 2, null, "Past duty"))).Success, "Started duty accepted");
        var transition = await Fixture.CreateAsync();
        Check((await transition.SubmitAsync(new(1, 2, null, "Transition test"))).Success, "Transition fixture submit failed");
        transition.Actor = 2; row = (await transition.Service.GetRequestsAsync()).Single();
        await using (var db = transition.Factory.CreateDbContext()) { db.RequestTypesAllowedRequestStateTransitions.Remove(await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(x => x.RequestTypeId == 71 && x.FromRequestStateId == 10 && x.ToRequestStateId == 3)); await db.SaveChangesAsync(); }
        Check(!(await transition.Service.ActAsync(row.Id, 10, "Accept")).Success, "Disallowed transition accepted");
        var declined = await Fixture.CreateAsync(); Check((await declined.SubmitAsync(new(1, 2, null, "Decline test"))).Success, "Decline fixture submit failed");
        declined.Actor = 2; row = (await declined.Service.GetRequestsAsync()).Single(); Check((await declined.Service.ActAsync(row.Id, 10, "Decline")).Success, "Decline failed");
        await using (var db = declined.Factory.CreateDbContext()) Check((await db.WorkAssignments.FindAsync(1L))!.IsValid, "Decline modified duty");
        var stages = await Fixture.CreateAsync();
        await using (var db = stages.Factory.CreateDbContext())
        {
            db.RequestStates.Add(new RequestState { RequestStateId = 14, StateName = "HR approval", IsProcessingState = true });
            db.WfWorkflowStages.Add(new WfWorkflowStage { WorkflowStageId = 99, WorkflowDefinitionId = 71, StageOrder = 2, StageName = "HR approval", RequestStateId = 14, ApprovalMode = "Any" });
            db.RequestTypesAllowedRequestStateTransitions.Remove(await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(x => x.RequestTypeId == 71 && x.FromRequestStateId == 3 && x.ToRequestStateId == 4));
            foreach(var pair in new[] { (3,14),(14,4),(14,5),(14,6) }) db.RequestTypesAllowedRequestStateTransitions.Add(new() { RequestTypeId = 71, FromRequestStateId = pair.Item1, ToRequestStateId = pair.Item2 });
            await db.SaveChangesAsync();
        }
        Check((await stages.SubmitAsync(new(1, 2, null, "Two-stage test"))).Success, "Multi-stage submission failed");
        stages.Actor = 2; row = (await stages.Service.GetRequestsAsync()).Single(); Check((await stages.Service.ActAsync(row.Id, 10, "Accept")).Success, "Multi-stage acceptance failed");
        stages.Actor = 3; Check((await stages.Service.ActAsync(row.Id, 3, "Approve")).Success, "First stage failed");
        await using(var db = stages.Factory.CreateDbContext()) Check((await db.WorkAssignments.FindAsync(1L))!.IsValid, "Duty moved before all stages completed");
        row = (await stages.Service.GetRequestsAsync()).Single(); Check(row.StateId == 14 && row.CanApprove, "Next stage did not become active");
        Check((await stages.Service.ActAsync(row.Id, 14, "Approve")).Success, "Final stage failed");
        var changed = await Fixture.CreateAsync(); Check((await changed.SubmitAsync(new(1, 2, null, "Changed duty"))).Success, "Changed duty submission failed");
        changed.Actor = 2; row = (await changed.Service.GetRequestsAsync()).Single();
        await using(var db = changed.Factory.CreateDbContext()) { (await db.WorkAssignments.FindAsync(1L))!.RequiresCheckOut = false; await db.SaveChangesAsync(); }
        Check(!(await changed.Service.ActAsync(row.Id, 10, "Accept")).Success, "Changed clock requirements were accepted without renewed consent");
        var required = await Fixture.CreateAsync(); Check((await required.SubmitAsync(new(1, 2, null, "Required comments"))).Success, "Comment fixture failed");
        await using(var db = required.Factory.CreateDbContext()) { (await db.RequestStates.FindAsync(3))!.CommentRequired = true; (await db.RequestStates.FindAsync(4))!.CommentRequired = true; await db.SaveChangesAsync(); }
        required.Actor = 2; row = (await required.Service.GetRequestsAsync()).Single();
        Check(!(await required.Service.ActAsync(row.Id, 10, "Accept")).Success, "Required acceptance comment ignored");
        Check((await required.Service.ActAsync(row.Id, 10, "Accept", "I accept this duty")).Success, "Acceptance with comment failed");
        required.Actor = 3; Check(!(await required.Service.ActAsync(row.Id, 3, "Approve")).Success, "Required approval comment ignored");
        Check((await required.Service.ActAsync(row.Id, 3, "Approve", "Approved duty cover")).Success, "Approval with comment failed");
        var cancelled = await Fixture.CreateAsync(); Check((await cancelled.SubmitAsync(new(1, 2, null, "Cancellation test"))).Success, "Cancellation fixture failed");
        row = (await cancelled.Service.GetRequestsAsync()).Single(); Check((await cancelled.Service.ActAsync(row.Id, 10, "Cancel")).Success, "Source cannot cancel");
        Check((await cancelled.SubmitAsync(new(1, 2, null, "Reservation released"))).Success, "Cancelled request kept duty reserved");
        var lateConflict = await Fixture.CreateAsync(); Check((await lateConflict.SubmitAsync(new(1, 2, null, "Late conflict"))).Success, "Late conflict fixture failed");
        lateConflict.Actor = 2; row = (await lateConflict.Service.GetRequestsAsync()).Single(); Check((await lateConflict.Service.ActAsync(row.Id, 10, "Accept")).Success, "Late conflict acceptance failed");
        await using(var db = lateConflict.Factory.CreateDbContext()) { db.WorkAssignments.Add(Assignment(3, 2, 200, lateConflict.Day, "Duty")); await db.SaveChangesAsync(); }
        lateConflict.Actor = 3; Check(!(await lateConflict.Service.ActAsync(row.Id, 3, "Approve")).Success, "Approval did not recheck duty conflicts");
        await using(var db = lateConflict.Factory.CreateDbContext()) Check((await db.WorkAssignments.FindAsync(1L))!.IsValid, "Blocked approval moved source duty");
        var ui = await Fixture.CreateAsync();
        using var providerUi = new ServiceCollection().AddLogging().AddSingleton<Microsoft.AspNetCore.Components.IComponentActivator, TestActivator>().BuildServiceProvider();
        await using var renderer = new TestRenderer(providerUi);
        var page = new HRM.Components.Pages.Online.DutyRoster();
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        page.GetType().GetProperty("Duties", flags)!.SetValue(page, ui.Service);
        page.GetType().GetField("date", flags)!.SetValue(page, ui.Day);
        await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(page); await renderer.Render(); }); renderer.CheckErrors();
        Check(renderer.RootText().Contains("Staff 1") && renderer.RootButtonCount("Transfer / Swap") == 1, "Roster did not show staff duty/action");
        await renderer.Dispatcher.InvokeAsync(async () => { await renderer.RootClick("Transfer / Swap");
            page.GetType().GetField("targetJob", flags)!.SetValue(page, 2); page.GetType().GetField("reason", flags)!.SetValue(page, "UI transfer");
            await renderer.Render(); await renderer.RootClick("Send for acceptance"); }); renderer.CheckErrors();
        Check((await ui.Service.GetRequestsAsync()).Count == 1, "Roster submission did not create the request");
    }

    static WorkAssignment Assignment(long id, int job, int person, DateTime day, string purpose) => new() {
        WorkAssignmentId = id, WorkTemplateId = 1, WorkTemplateTypeId = 1, WorkAssignmentStateId = 7, Name = purpose + " test", SchedulePurpose = purpose,
        StartDateTime = day.AddHours(10), EndDateTime = day.AddHours(12), IsValid = true, RequiresAttendance = true, RequiresCheckOut = true,
        WorkPlan = new WorkPlan { JobId = job, IndividualId = person, OrganisationBusinessEntityId = 10, WorkDate = day, IsValid = true, PlanningProviderId = 1, IsGenerated = true, PlanGuid = Guid.NewGuid() },
        WorkAssignmentOwners = [new() { JobId = job, IndividualId = person, IsCurrentOwner = true, IsValid = true }],
        WorkAssignmentSegments = [new() { WorkTemplateSegmentId = 10, WorkSegmentTypeId = 1, Name = "In", SequenceNumber = 1, StartDateTime = day.AddHours(10), EndDateTime = day.AddHours(10.5), IsValid = true, IsMandatory = true, RequiresAttendance = true },
            new() { WorkTemplateSegmentId = 11, WorkSegmentTypeId = 2, Name = "Out", SequenceNumber = 2, StartDateTime = day.AddHours(12), EndDateTime = day.AddHours(12.5), IsValid = true, IsMandatory = true, RequiresAttendance = true }] };

    sealed class Fixture
    {
        public int Actor = 1; public bool Eligible = true; public DateTime Day = DateTime.UtcNow.AddHours(5).Date.AddDays(4);
        public ReceiptFactory Factory = null!; public DutyRosterService Service = null!;
        public async Task<HRM.DTOs.ServiceResult> SubmitAsync(DutyChangeApplication model)
        {
            var result = await Service.SubmitAsync(model);
            if (!result.Success) return result;
            var previousActor = Actor; Actor = 1;
            var row = (await Service.GetRequestsAsync()).First(x => x.StateId == 10 && x.CanAccept);
            var accepted = await Service.ActAsync(row.Id, 10, "Accept", "I accept the proposed duty change");
            Actor = previousActor; Check(accepted.Success, accepted.Message); return result;
        }
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            f.Factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
            var access = Proxy.For<IUserAccessService>((method, args) => method switch {
                "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = f.Actor, IndividualId = f.Actor * 100 }),
                "GetCurrentOrganisationIdAsync" => Task.FromResult(10), "GetCurrentJobIdAsync" => Task.FromResult(f.Actor),
                "HasOrganisationAccessAsync" => Task.FromResult((int)args[0]! == 10), "HasPermissionAsync" => Task.FromResult(false), _ => throw new Exception(method) });
            var log = Proxy.For<IOperationLogService>((_, args) => Log((HrmTeContext)args[0]!));
            var planner = Proxy.For<IWorkPlanGenerator>((_, _) => Task.FromResult<HRM.DTOs.Attendance.AttendanceWorkPlanDto?>(null));
            var resolver = Proxy.For<IWorkflowApproverResolver>((_, _) => Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>(f.Eligible ? [new(3, 1, 1, true)] : []));
            f.Service = new DutyRosterService(f.Factory, access, log, planner, resolver, new WorkflowRequestContextResolver(f.Factory));
            await using var db = f.Factory.CreateDbContext();
            db.Organisations.Add(new Organisation { BusinessEntityID = 10, OrganisationName = "Test tenant" });
            db.UserOrganisations.Add(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 });
            for (int i = 1; i <= 3; i++) {
                db.Individuals.Add(new Individual { BusinessEntityId = i * 100, FirstNameEnglish = "Staff " + i });
                db.Jobs.Add(new Job { JobId = i, IndividualID = i * 100, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = f.Day.AddYears(-1), OrganisationStructureId = 1 });
                db.Users.Add(new User { UserId = i, Username = "staff" + i, BusinessEntityID = i * 100, UserOrganisationId = 1, IsActive = true, LocalUserStateId = 1 });
            }
            db.JobTypes.Add(new JobType { JobTypeId = 1, TypeName = "Contract", IsActive = true });
            db.OrganisationStructures.Add(new OrganisationStructure { OrganisationStructureId = 1, Name = "Team" });
            foreach (var id in new[] { 1, 3, 4, 5, 6, 10 }) db.RequestStates.Add(new RequestState { RequestStateId = id, StateName = "State " + id, IsFinalState = id is 4 or 5 or 6, IsProcessingState = id is 1 or 3 or 10 });
            foreach (var id in new[] { 71, 72 }) {
                db.RequestTypes.Add(new RequestType { RequestTypeId = id, TypeName = id == 71 ? DutyRosterService.TransferType : DutyRosterService.SwapType, ViewUrl = "/online/duty-roster" });
                foreach (var (from, to) in new[] { (1,10),(10,3),(10,5),(10,6),(3,4),(3,5),(3,6) }) db.RequestTypesAllowedRequestStateTransitions.Add(new() { RequestTypeId = id, FromRequestStateId = from, ToRequestStateId = to });
                db.WfWorkflowDefinitions.Add(new WfWorkflowDefinition { WorkflowDefinitionId = id, RequestTypeId = id, OrganisationBusinessEntityId = 10, LeaveRequestKind = 0, Name = "Supervisor", IsActive = true,
                    Stages = [new() { WorkflowStageId = id, StageOrder = 1, StageName = "Supervisor approval", RequestStateId = 3, ApprovalMode = "Any",
                        Approvers = [new() { StageApproverId = id, ApproverType = "User", ApproverReferenceId = 3, IsRequired = true }] }] });
            }
            db.WorkTemplates.Add(new WorkTemplate { WorkTemplateId = 1, WorkTemplateTypeId = 1, IsActive = true, OrganisationBusinessEntityId = 10, Name = "Duty", DefaultStartTime = new(10,0), DefaultEndTime = new(12,0),
                WorkTemplateSegments = [new() { WorkTemplateSegmentId = 10, WorkSegmentTypeId = 1, IsActive = true, Name = "In", DurationMinutes = 30, SequenceNumber = 1 }, new() { WorkTemplateSegmentId = 11, WorkSegmentTypeId = 2, IsActive = true, Name = "Out", OffsetMinutes = 120, DurationMinutes = 30, SequenceNumber = 2 }] });
            db.WorkAssignmentStates.Add(new WorkAssignmentState { WorkAssignmentStateId = 7, Code = "PLANNED", IsActive = true });
            db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment { JobID = 1, WorkTemplateID = 1, SchedulePurpose = "Duty", ScheduleMode = "Standard", EffectiveFrom = f.Day, IsActive = true });
            var source = Assignment(1, 1, 100, f.Day, "Duty"); db.WorkAssignments.Add(source);
            var official = Assignment(2, 1, 100, f.Day, "Official"); official.WorkPlan = source.WorkPlan; db.WorkAssignments.Add(official);
            foreach(var segment in source.WorkAssignmentSegments) source.WorkPlan.WorkPlanSegments.Add(new WorkPlanSegment { WorkTemplateSegmentId = segment.WorkTemplateSegmentId, WorkSegmentTypeId = segment.WorkSegmentTypeId,
                SchedulePurpose = "Duty", Name = segment.Name, StartDateTime = segment.StartDateTime, EndDateTime = segment.EndDateTime!.Value, IsValid = true });
            await db.SaveChangesAsync(); return f;
        }
        static async Task<OperationLog> Log(HrmTeContext db) { var audit = new OperationLog(); db.OperationLogs.Add(audit); await db.SaveChangesAsync(); return audit; }
    }
}
