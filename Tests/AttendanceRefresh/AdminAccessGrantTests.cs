using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class AdminAccessGrantTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static async Task Denied(Func<Task> action)
    {
        try { await action(); } catch (UnauthorizedAccessException) { return; }
        throw new Exception("Expected access denial");
    }
    public static async Task RunAsync()
    {
        var f = await Fixture.Create();
        var result = await f.Service.SubmitAsync(10, 4, [3], [2], "Staff needs HR portal access");
        Check(result.Success, result.Message);
        var row = (await f.Service.LoadAsync()).Requests.Single();
        Check(row.StateId == 2 && row.Status.StartsWith("Processing"), "Initial processing state missing");
        await using (var db = f.Factory.CreateDbContext())
        {
            Check(!(await db.Users.SingleAsync(u => u.UserId == 4)).AdminPortalAccessEnabled, "Submission activated access");
            Check(!await db.UserAssignedUserGroups.AnyAsync(g => g.UserId == 4) && !await db.UserRoles.AnyAsync(g => g.UserId == 4), "Submission assigned privileges before approval");
            Check(await db.RequestNotifications.AnyAsync(n => n.UserId == 2 && n.NotificationKind == "ActionRequired"), "First approver notification missing");
        }
        Check(!(await f.Service.SubmitAsync(10, 4, [3], [2], "Duplicate")).Success, "Duplicate access request accepted");
        f.Actor = 4;
        var own = await f.Service.LoadAsync();
        Check(!own.CanManage && own.Requests.Count == 1 && !own.Requests[0].CanApprove, "Recipient cannot view request or can approve own access");
        await Denied(() => f.Service.ActAsync(10, row.Id, 2, "Approve", "Self approval"));
        f.Actor = 3;
        await Denied(() => f.Service.ActAsync(10, row.Id, 2, "Approve", "Skip stage"));
        f.Actor = 2;
        Check((await f.Service.ActAsync(10, row.Id, 2, "Approve", "Verified")).Success, "First stage failed");
        await using (var db = f.Factory.CreateDbContext()) Check(!(await db.Users.SingleAsync(u => u.UserId == 4)).AdminPortalAccessEnabled, "Intermediate approval granted access");
        f.Actor = 3;
        Check(!(await f.Service.ActAsync(10, row.Id, 2, "Approve", "Stale")).Success, "Stale state approved");
        Check((await f.Service.ActAsync(10, row.Id, 3, "Approve", "Approved")).Success, "Final stage failed");
        Check(!(await f.Service.ActAsync(10, row.Id, 3, "Approve", "Replay")).Success, "Approval replay accepted");
        await using (var db = f.Factory.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(u => u.UserId == 4);
            Check(user.AdminPortalAccessEnabled && user.AdminPortalAccessOrganisationId == 1, "Approved access not organisation scoped");
            Check(await db.UserAssignedUserGroups.AnyAsync(g => g.UserId == 4 && g.UserGroupId == 3), "Approved group not assigned");
            Check(await db.UserRoles.AnyAsync(g => g.UserId == 4 && g.RoleId == 2 && g.IsActive), "Approved permission not assigned");
            Check((await db.WfRequestInstances.SingleAsync()).ExecutionStatus == "Succeeded" && !await db.AdminAccessGrantRequests.AnyAsync(r => r.IsOpen), "Workflow not finalized");
            Check(await db.WfRequestActions.CountAsync() == 3, "Workflow audit incomplete");
        }
        f.Actor = 1;
        Check((await f.Service.RevokeAsync(10, 4, "Access no longer required")).Success, "Revoke failed");
        Check(!(await new EffectivePermissionService(f.Factory).GetEffectivePermissionsAsync(4)).Any(), "Revoke retained Admin permissions");
        await using (var db = f.Factory.CreateDbContext()) Check(await db.StaffNotifications.AnyAsync(n => n.UserId == 4 && n.Kind == "AdminAccess"), "Revocation notification missing");
        Check(!(await f.Service.SubmitAsync(10, 1, [3], [2], "Own access")).Success, "Self request accepted");
        await Denied(() => f.Service.LoadAsync(20));
        await Denied(() => f.Service.SubmitAsync(10, 5, [3], [2], "Foreign account"));
        await Denied(() => f.Service.SubmitAsync(10, 4, [3], [3], "Escalate to Super Admin"));
        f.Super = true;
        Check((await f.Service.LoadAsync(20)).OrganisationId == 20, "Super Admin cannot select another organisation");

        var rejected = await Fixture.Create();
        Check((await rejected.Service.SubmitAsync(10, 4, [3], [2], "Request")).Success, "Reject fixture failed");
        row = (await rejected.Service.LoadAsync()).Requests.Single(); rejected.Actor = 2;
        Check((await rejected.Service.ActAsync(10, row.Id, 2, "Reject", "Not required")).Success, "Reject failed");
        await using (var db = rejected.Factory.CreateDbContext()) Check(!(await db.Users.SingleAsync(u => u.UserId == 4)).AdminPortalAccessEnabled && (await db.Requests.SingleAsync()).RequestStateId == 6, "Rejected request granted access");

        var cancelled = await Fixture.Create();
        Check((await cancelled.Service.SubmitAsync(10, 4, [3], [2], "Request")).Success, "Cancel fixture failed");
        row = (await cancelled.Service.LoadAsync()).Requests.Single();
        Check((await cancelled.Service.ActAsync(10, row.Id, 2, "Cancel", "Withdraw")).Success, "Cancel failed");
        await using (var db = cancelled.Factory.CreateDbContext()) Check(!(await db.Users.SingleAsync(u => u.UserId == 4)).AdminPortalAccessEnabled, "Cancellation granted access");

        var missing = await Fixture.Create();
        var limited = await Fixture.Create();
        await using (var db = limited.Factory.CreateDbContext()) { (await db.UserOrganisationRoles.SingleAsync(p => p.RoleID == 2)).IsActive = false; await db.SaveChangesAsync(); }
        Check(!(await limited.Service.SubmitAsync(10, 4, [3], [2], "Unavailable organisation permission")).Success, "Admin access grant exceeded the organisation ceiling");
        await using (var db = missing.Factory.CreateDbContext()) { db.RequestTypesAllowedRequestStateTransitions.Remove(await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(t => t.FromRequestStateId == 3 && t.ToRequestStateId == 4)); await db.SaveChangesAsync(); }
        Check(!(await missing.Service.SubmitAsync(10, 4, [3], [2], "Request")).Success, "Missing allowed transition accepted");
        var noFlow = await Fixture.Create();
        await using (var db = noFlow.Factory.CreateDbContext()) { (await db.WfWorkflowDefinitions.SingleAsync()).IsActive = false; await db.SaveChangesAsync(); }
        Check(!(await noFlow.Service.SubmitAsync(10, 4, [3], [2], "Request")).Success, "Missing flow bypassed approval");
        var removed = await Fixture.Create();
        Check((await removed.Service.SubmitAsync(10, 4, [3], [2], "Request")).Success, "Membership fixture failed");
        row = (await removed.Service.LoadAsync()).Requests.Single(); removed.Actor = 2;
        await using (var db = removed.Factory.CreateDbContext()) { db.UserAssignedUserGroups.Remove(await db.UserAssignedUserGroups.SingleAsync(g => g.UserId == 2)); await db.SaveChangesAsync(); }
        await Denied(() => removed.Service.ActAsync(10, row.Id, 2, "Approve", "Removed membership"));
    }
    private sealed class Fixture
    {
        public int Actor = 1;
        public bool Super;
        public ReceiptFactory Factory = null!;
        public AdminAccessGrantService Service = null!;
        public static async Task<Fixture> Create()
        {
            var f = new Fixture { Factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options) };
            var access = Proxy.For<IUserAccessService>((m, a) => m switch {
                "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = f.Actor, IndividualId = f.Actor * 100, IsSuperAdministrator = f.Super }),
                "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
                "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == 10),
                "HasPermissionAsync" => Task.FromResult((string)a[0]! == "Admin" && f.Actor == 1 || (string)a[0]! == "Super.Admin.Manage" && f.Super),
                _ => throw new Exception(m) });
            var logs = Proxy.For<IOperationLogService>((m, a) => m == "CreateAsync" ? Log((HrmTeContext)a[0]!) : throw new Exception(m));
            var contexts = Proxy.For<IUserContextService>((m, a) => m == "InvalidateAsync" ? Task.CompletedTask : throw new Exception(m));
            f.Service = new(f.Factory, access, logs, new WorkflowApproverResolver(f.Factory), contexts);
            await using var db = f.Factory.CreateDbContext();
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Other" });
            db.UserOrganisations.AddRange(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 }, new UserOrganisation { UserOrganisationID = 2, BusinessEntityID = 20 });
            for (var id = 1; id <= 5; id++)
            {
                db.Individuals.Add(new Individual { BusinessEntityId = id * 100, FirstNameEnglish = "Person " + id });
                db.Users.Add(new User { UserId = id, BusinessEntityID = id * 100, Username = "user" + id, FullName = "Person " + id,
                    UserOrganisationId = id == 5 ? 2 : 1, IsActive = true, LocalUserStateId = 1,
                    AdminPortalAccessEnabled = id < 4, AdminPortalAccessOrganisationId = id < 4 ? 1 : null });
            }
            db.JobTypes.Add(new JobType { JobTypeId = 1, TypeName = "Contract", IsActive = true });
            db.Jobs.Add(new Job { JobId = 1, IndividualID = 400, OrganisationID = 10, JobTypeId = 1, JobStateId = 4, JoinedDate = DateTime.Today.AddYears(-1) });
            db.Roles.AddRange(new Role { RoleID = 1, RoleKey = "Admin", Name = "Admin", ContextId = 1 }, new Role { RoleID = 2, RoleKey = "Users.View", Name = "View users", ContextId = 1 }, new Role { RoleID = 3, RoleKey = "Super.Admin.Manage", Name = "Super Admin", ContextId = 1 });
            for (var id = 1; id <= 3; id++) db.UserOrganisationRoles.Add(new UserOrganisationRole { UserOrganisationID = 1, RoleID = id, IsActive = true });
            for (var id = 1; id <= 3; id++) db.UserGroups.Add(new UserGroup { UserGroupId = id, GroupName = "Group " + id, UserOrganisationId = 1 });
            db.UserGroupRoles.Add(new UserGroupRole { UserGroupId = 3, RoleId = 2, IsActive = true });
            db.UserAssignedUserGroups.AddRange(new UserAssignedUserGroup { UserId = 2, UserGroupId = 1 }, new UserAssignedUserGroup { UserId = 3, UserGroupId = 2 });
            db.RequestTypes.Add(new RequestType { RequestTypeId = 40, TypeName = AdminAccessGrantService.TypeName, ViewUrl = AdminAccessGrantService.PageUrl });
            for (var id = 1; id <= 6; id++) db.RequestStates.Add(new RequestState { RequestStateId = id, StateName = "State " + id, IsProcessingState = id < 4, IsFinalState = id >= 4 });
            foreach (var (from, to) in new[] { (1, 2), (2, 3), (3, 4), (2, 5), (3, 5), (2, 6), (3, 6) }) db.RequestTypesAllowedRequestStateTransitions.Add(new() { RequestTypeId = 40, FromRequestStateId = from, ToRequestStateId = to });
            db.WfWorkflowDefinitions.Add(new WfWorkflowDefinition { WorkflowDefinitionId = 1, RequestTypeId = 40, OrganisationBusinessEntityId = 10, LeaveRequestKind = 0, IsActive = true, Name = "Admin access grant",
                WorkflowJobTypes = [new() { JobTypeId = 1, IsActive = true }], Stages = [
                    new() { WorkflowStageId = 1, StageOrder = 1, StageName = "Verify", RequestStateId = 2, ApprovalMode = "Any", Approvers = [new() { StageApproverId = 1, ApproverType = "UserGroup", ApproverReferenceId = 1, IsRequired = true }] },
                    new() { WorkflowStageId = 2, StageOrder = 2, StageName = "Approve", RequestStateId = 3, ApprovalMode = "Any", Approvers = [new() { StageApproverId = 2, ApproverType = "UserGroup", ApproverReferenceId = 2, IsRequired = true }] } ] });
            await db.SaveChangesAsync(); return f;
        }
        private static async Task<OperationLog> Log(HrmTeContext db)
        {
            var log = new OperationLog { CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, CreatedByIp = "", UpdatedByIp = "" };
            db.OperationLogs.Add(log); await db.SaveChangesAsync(); return log;
        }
    }
}
