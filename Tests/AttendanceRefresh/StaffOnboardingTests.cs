using HRM.DTOs;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

static class StaffOnboardingTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var formFixture = await Fixture.CreateAsync();
        await using(var db = formFixture.Factory.CreateDbContext())
        {
            db.Positions.Add(new Position { PositionId = 1, Name = "Selected appointment position", OrganisationId = 10, IsActive = true });
            await db.SaveChangesAsync();
        }
        using (var provider = new ServiceCollection().AddLogging().AddSingleton<IComponentActivator, TestActivator>().BuildServiceProvider())
        {
            await using var renderer = new TestRenderer(provider);
            var page = new HRM.Components.Admin.Hr.Staff.StaffOnboardingPage();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var portal = new PortalContext(); portal.Initialize(10, "Tenant", true);
            page.GetType().GetProperty("Onboarding", flags)!.SetValue(page, formFixture.Service);
            page.GetType().GetProperty("Portal", flags)!.SetValue(page, portal);
            page.GetType().GetProperty("Logger", flags)!.SetValue(page, NullLogger<HRM.Components.Admin.Hr.Staff.StaffOnboardingPage>.Instance);
            page.IndividualId = 300;
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(page); await renderer.Render(); }); renderer.CheckErrors();
            var edited = (StaffOnboardingForm)page.GetType().GetField("form", flags)!.GetValue(page)!;
            edited.PositionId = 1; edited.OrganisationStructureId = 1; edited.JobTypeId = 1; edited.Remarks = "Keep my selections";
            await renderer.Dispatcher.InvokeAsync(() => renderer.Render()); renderer.CheckErrors();
            var retained = (StaffOnboardingForm)page.GetType().GetField("form", flags)!.GetValue(page)!;
            Check(ReferenceEquals(edited, retained) && retained.PositionId == 1 && retained.Remarks == "Keep my selections" &&
                renderer.RootText().Contains("Selected appointment position"), "Parent render cleared the selected position or appointment fields");
            page.RequestId = 123;
            await renderer.Dispatcher.InvokeAsync(() => renderer.Render());
            renderer.CheckErrors();
            Check(((StaffOnboardingForm)page.GetType().GetField("form", flags)!.GetValue(page)!).PositionId == null,
                "Opening a different request retained the prior appointment selection");
        }
        var f = await Fixture.CreateAsync();
        var result = await f.Service.SubmitAsync(f.Form()); Check(result.Success, result.Message);
        var row = (await f.Service.GetRequestsAsync()).Single();
        Check(row.Status.StartsWith("Processing") && row.StateId == 2 && row.CanCancel && !row.CanApprove, "Incorrect initial processing/creator access");
        await f.AssertNoAccountAsync();
        Check(!(await f.Service.SubmitAsync(f.Form())).Success, "Duplicate onboarding accepted");
        Check(!(await f.Service.ActAsync(row.RequestId, 2, "Approve", null)).Success, "Creator approved own submission");
        f.Actor = 3;
        Check(!(await f.Service.ActAsync(row.RequestId, 2, "Approve", null)).Success, "Final approver skipped the first stage");
        f.Actor = 2;
        Check((await f.Service.GetRequestsAsync()).Single().CanApprove, "Assigned first-stage approver cannot act");
        Check((await f.Service.ActAsync(row.RequestId, 2, "Approve", "Verified appointment")).Success, "First approval failed");
        await f.AssertNoAccountAsync();
        f.Actor = 3;
        Check(!(await f.Service.ActAsync(row.RequestId, 2, "Approve", null)).Success, "Stale state accepted");
        row = (await f.Service.GetRequestsAsync()).Single();
        Check(row.StateId == 3 && row.CanApprove, "Request did not advance to the second stage");
        Check((await f.Service.ActAsync(row.RequestId, 3, "Approve", "Approved")).Success, "Final approval failed");
        Check(!(await f.Service.ActAsync(row.RequestId, 3, "Approve", "Again")).Success, "Final approval ran twice");
        await using (var db = f.Factory.CreateDbContext())
        {
            var account = await db.Users.SingleAsync(x => x.BusinessEntityID == 300);
            Check(account.Username == "A747888" && account.UserOrganisationId == 1 && account.IsActive && account.IsApproved == true, "Wrong new account identity, tenant or state");
            Check(await db.Staffs.AnyAsync(x => x.IndividualId == 300), "Staff record missing");
            var job = await db.Jobs.SingleAsync(x => x.IndividualID == 300);
            Check(job.JobStateId == 4 && job.OrganisationID == 10 && job.JobTypeId == 1 && job.OrganisationStructureId == 1, "Incorrect approved employment");
            Check(!await db.JobPositions.AnyAsync(x => x.JobId == job.JobId), "An optional position was invented");
            Check(!await db.UserRoles.AnyAsync(x => x.UserId == account.UserId) && !await db.UserAssignedUserGroups.AnyAsync(x => x.UserId == account.UserId), "Onboarding granted elevated roles");
            var onboarding = await db.StaffOnboardingRequests.SingleAsync();
            Check(!onboarding.IsOpen && onboarding.UserId == account.UserId && onboarding.JobId == job.JobId, "Approval links not recorded");
            Check(await db.RequestNotifications.AnyAsync(x => x.UserId == 2 && x.NotificationKind == "ActionRequired") &&
                await db.RequestNotifications.AnyAsync(x => x.UserId == 3 && x.NotificationKind == "ActionRequired"), "Assigned approver notifications missing");
            Check(await db.StaffNotifications.AnyAsync(x => x.UserId == account.UserId && x.Kind == "Onboarding"), "New staff notification missing");
            Check(await db.WfRequestActions.CountAsync() == 3, "Submit and approval history missing");
            Check(await db.WfRequestStageInstances.AllAsync(x => x.StageStatus == "Completed") &&
                (await db.WfRequestInstances.SingleAsync()).ExecutionStatus == "Succeeded", "Approval wrote statuses forbidden by the SQL workflow constraints");
        }

        var rejected = await Fixture.CreateAsync();
        Check((await rejected.Service.SubmitAsync(rejected.Form())).Success, "Rejection fixture failed");
        row = (await rejected.Service.GetRequestsAsync()).Single(); rejected.Actor = 2;
        Check(!(await rejected.Service.ActAsync(row.RequestId, 2, "Reject", null)).Success, "Rejection without reason accepted");
        Check((await rejected.Service.ActAsync(row.RequestId, 2, "Reject", "Incorrect appointment")).Success, "Rejection failed");
        await rejected.AssertNoAccountAsync();
        await using(var db = rejected.Factory.CreateDbContext()) Check((await db.Requests.SingleAsync()).RequestStateId == 6 && !(await db.StaffOnboardingRequests.SingleAsync()).IsOpen, "Rejected onboarding remains processing");

        var cancelled = await Fixture.CreateAsync();
        Check((await cancelled.Service.SubmitAsync(cancelled.Form())).Success, "Cancellation fixture failed");
        row = (await cancelled.Service.GetRequestsAsync()).Single();
        Check((await cancelled.Service.ActAsync(row.RequestId, 2, "Cancel", "Withdraw appointment")).Success, "Creator could not cancel");
        await cancelled.AssertNoAccountAsync();
        await using(var db = cancelled.Factory.CreateDbContext())
            Check(await db.WfRequestApprovals.AllAsync(x => x.Decision != "Cancelled"), "Cancellation wrote a decision forbidden by the SQL workflow constraints");

        var transitions = await Fixture.CreateAsync();
        await using(var db = transitions.Factory.CreateDbContext()) { db.RequestTypesAllowedRequestStateTransitions.Remove(await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(x => x.FromRequestStateId == 3 && x.ToRequestStateId == 4)); await db.SaveChangesAsync(); }
        Check(!(await transitions.Service.SubmitAsync(transitions.Form())).Success, "Missing final transition accepted");
        await transitions.AssertNoAccountAsync();

        var revoked = await Fixture.CreateAsync();
        Check((await revoked.Service.SubmitAsync(revoked.Form())).Success, "Revocation fixture failed");
        row = (await revoked.Service.GetRequestsAsync()).Single(); revoked.Actor = 2;
        await using(var db = revoked.Factory.CreateDbContext()) { db.UserAssignedUserGroups.Remove(await db.UserAssignedUserGroups.SingleAsync(x => x.UserId == 2)); await db.SaveChangesAsync(); }
        Check(!(await revoked.Service.ActAsync(row.RequestId, 2, "Approve", null)).Success, "Removed group membership retained approval rights");

        var tenant = await Fixture.CreateAsync();
        var foreign = tenant.Form(); foreign.OrganisationStructureId = 2;
        Check(!(await tenant.Service.SubmitAsync(foreign)).Success, "Foreign department accepted");
        tenant.Org = 20;
        Check(!(await tenant.Service.SubmitAsync(tenant.Form())).Success, "Foreign individual accepted");
        tenant.Org = 10; tenant.Actor = 2;
        Check(!(await tenant.Service.SubmitAsync(tenant.Form())).Success, "Non-HR staff submitted onboarding");

        var missingFlow = await Fixture.CreateAsync();
        await using(var db = missingFlow.Factory.CreateDbContext()) { (await db.WfWorkflowDefinitions.SingleAsync()).IsActive = false; await db.SaveChangesAsync(); }
        Check(!(await missingFlow.Service.SubmitAsync(missingFlow.Form())).Success, "Unconfigured approval flow auto-approved");

        var supervisor = await Fixture.CreateAsync();
        await using(var db = supervisor.Factory.CreateDbContext())
        {
            var rule = await db.WfStageApprovers.SingleAsync(x => x.StageApproverId == 1); rule.ApproverType = "SupervisorGroup";
            db.OrganisationStructureSupervisors.Add(new OrganisationStructureSupervisor { OrganisationStructureSupervisorId = 1, OrganisationStructureId = 1, SupervisorStaffId = 200, IsActive = true, IsValid = true });
            await db.SaveChangesAsync();
        }
        Check((await supervisor.Service.SubmitAsync(supervisor.Form())).Success, "Proposed department supervisor could not resolve before employment exists");

        var reuse = await Fixture.CreateAsync();
        await using(var db = reuse.Factory.CreateDbContext())
        {
            db.Users.Add(new User { UserId = 9, BusinessEntityID = 300, Username = "A747888", UserOrganisationId = 1, IsActive = false, LocalUserStateId = 4, Email = "existing@example.test" });
            db.Positions.Add(new Position { PositionId = 1, Name = "Existing position", OrganisationId = 10, IsActive = true });
            await db.SaveChangesAsync();
        }
        var positioned = reuse.Form(); positioned.PositionId = 1;
        Check((await reuse.Service.SubmitAsync(positioned)).Success, "Existing account/position submission failed");
        row = (await reuse.Service.GetRequestsAsync()).Single(); reuse.Actor = 2;
        Check((await reuse.Service.ActAsync(row.RequestId, 2, "Approve", "Verified")).Success, "Existing account first approval failed");
        reuse.Actor = 3;
        Check((await reuse.Service.ActAsync(row.RequestId, 3, "Approve", "Approved")).Success, "Existing account final approval failed");
        await using(var db = reuse.Factory.CreateDbContext())
        {
            var account = await db.Users.SingleAsync(x => x.BusinessEntityID == 300);
            Check(account.UserId == 9 && !account.IsActive && account.LocalUserStateId == 4 && account.Email == "existing@example.test", "Existing account duplicated or restrictions/contact data overwritten");
            var position = await db.JobPositions.SingleAsync();
            Check(position.PositionId == 1 && position.JobPositionStateId == 4 && await db.JobPositionsRequests.AnyAsync(x => x.JobPositionId == position.JobPositionId), "Existing optional position was not assigned and linked after approval");
        }

        var submittingHr = await Fixture.CreateAsync();
        await using(var db = submittingHr.Factory.CreateDbContext()) { db.UserAssignedUserGroups.Add(new UserAssignedUserGroup { UserId = 1, UserGroupId = 1 }); await db.SaveChangesAsync(); }
        Check((await submittingHr.Service.SubmitAsync(submittingHr.Form())).Success, "Submitting HR fixture failed");
        row = (await submittingHr.Service.GetRequestsAsync()).Single();
        Check(row.CanApprove, "Configured HR approver could not approve another person's appointment because they submitted it");
        Check((await submittingHr.Service.ActAsync(row.RequestId, 2, "Approve", "Verified another employee")).Success, "Eligible submitting HR could not approve the stage");

        var ownAppointment = await Fixture.CreateAsync();
        await using(var db = ownAppointment.Factory.CreateDbContext())
        {
            db.Idcards.Add(new Idcard { BusinessEntityId = 100, IdcardNumber = "user1" });
            db.UserAssignedUserGroups.Add(new UserAssignedUserGroup { UserId = 1, UserGroupId = 1 });
            await db.SaveChangesAsync();
        }
        var ownForm = ownAppointment.Form(); ownForm.IndividualId = 100;
        Check((await ownAppointment.Service.SubmitAsync(ownForm)).Success, "Own appointment submission fixture failed");
        row = (await ownAppointment.Service.GetRequestsAsync()).Single();
        Check(!row.CanApprove && !(await ownAppointment.Service.ActAsync(row.RequestId, 2, "Approve", "Self")).Success, "HR approved their own appointment");
    }

    private sealed class Fixture
    {
        public ReceiptFactory Factory = null!;
        public StaffOnboardingService Service = null!;
        public int Actor = 1, Org = 10;
        public StaffOnboardingForm Form() => new() { IndividualId = 300, OrganisationStructureId = 1, JobTypeId = 1, JoinedDate = new(2026, 10, 22), Remarks = "New appointment" };
        public async Task AssertNoAccountAsync()
        {
            await using var db = Factory.CreateDbContext();
            Check(!await db.Users.AnyAsync(x => x.BusinessEntityID == 300) && !await db.Jobs.AnyAsync(x => x.IndividualID == 300), "Account or employment created before final approval");
        }
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            f.Factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
            var access = Proxy.For<IUserAccessService>((method, args) => method switch
            {
                "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = f.Actor, IndividualId = f.Actor == 3 ? 400 : f.Actor * 100, IsHRStaff = f.Actor == 1 }),
                "GetCurrentOrganisationIdAsync" => Task.FromResult(f.Org),
                "HasOrganisationAccessAsync" => Task.FromResult((int)args[0]! == f.Org),
                "HasPermissionAsync" => Task.FromResult(false), _ => throw new Exception(method)
            });
            var logs = Proxy.For<IOperationLogService>((method, args) => method == "CreateAsync" ? LogAsync((HrmTeContext)args[0]!) : throw new Exception(method));
            f.Service = new(f.Factory, access, logs, new WorkflowApproverResolver(f.Factory));
            await using var db = f.Factory.CreateDbContext();
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Other" });
            db.UserOrganisations.AddRange(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 }, new UserOrganisation { UserOrganisationID = 2, BusinessEntityID = 20 });
            foreach (var id in new[] { 100, 200, 300, 400 }) db.Individuals.Add(new Individual { BusinessEntityId = id, FirstNameEnglish = "Person", LastNameEnglish = id.ToString() });
            foreach (var id in new[] { 1, 2, 3 }) db.Users.Add(new User { UserId = id, Username = "user" + id, BusinessEntityID = id == 3 ? 400 : id * 100, UserOrganisationId = 1, IsActive = true, LocalUserStateId = 1 });
            db.Idcards.Add(new Idcard { BusinessEntityId = 300, IdcardNumber = "A747888" });
            db.JobTypes.Add(new JobType { JobTypeId = 1, TypeName = "Contract", IsActive = true });
            db.OrganisationStructureTypes.Add(new OrganisationStructureType { OrganisationStructureTypeId = 1, TypeName = "Department" });
            db.OrganisationStructures.AddRange(new OrganisationStructure { OrganisationStructureId = 1, OrganisationBusinessEntityID = 10, OrganisationStructureTypeId = 1, OrganisationStructureStateId = 3, Name = "Department" }, new OrganisationStructure { OrganisationStructureId = 2, OrganisationBusinessEntityID = 20, OrganisationStructureTypeId = 1, OrganisationStructureStateId = 3, Name = "Foreign" });
            db.RequestTypes.Add(new RequestType { RequestTypeId = 32, TypeName = StaffOnboardingService.RequestTypeName, ViewUrl = StaffOnboardingService.PageUrl });
            foreach (var id in new[] { 1, 2, 3, 4, 5, 6 }) db.RequestStates.Add(new RequestState { RequestStateId = id, StateName = "State " + id, IsProcessingState = id <= 3, IsFinalState = id >= 4 });
            foreach (var (from, to) in new[] { (1, 2), (2, 3), (3, 4), (2, 5), (3, 5), (2, 6), (3, 6) })
                db.RequestTypesAllowedRequestStateTransitions.Add(new() { RequestTypeId = 32, FromRequestStateId = from, ToRequestStateId = to });
            for (var id = 1; id <= 2; id++)
            {
                db.UserGroups.Add(new UserGroup { UserGroupId = id, GroupName = "Approvers " + id, UserOrganisationId = 1 });
                db.UserAssignedUserGroups.Add(new UserAssignedUserGroup { UserGroupId = id, UserId = id + 1 });
            }
            db.WfWorkflowDefinitions.Add(new WfWorkflowDefinition { WorkflowDefinitionId = 1, RequestTypeId = 32, OrganisationBusinessEntityId = 10, LeaveRequestKind = 0, Name = "Appointment", IsActive = true,
                WorkflowJobTypes = [new() { JobTypeId = 1, IsActive = true }],
                Stages = [new() { WorkflowStageId = 1, StageOrder = 1, StageName = "Verify", RequestStateId = 2, ApprovalMode = "Any", Approvers = [new() { StageApproverId = 1, ApproverType = "UserGroup", ApproverReferenceId = 1, IsRequired = true }] },
                    new() { WorkflowStageId = 2, StageOrder = 2, StageName = "Approve", RequestStateId = 3, ApprovalMode = "Any", Approvers = [new() { StageApproverId = 2, ApproverType = "UserGroup", ApproverReferenceId = 2, IsRequired = true }] }] });
            await db.SaveChangesAsync(); return f;
        }
        private static async Task<OperationLog> LogAsync(HrmTeContext db)
        {
            var log = new OperationLog { CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, CreatedByIp = "", UpdatedByIp = "" };
            db.OperationLogs.Add(log); await db.SaveChangesAsync(); return log;
        }
    }
}
