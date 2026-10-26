using HRM.DTOs.UserContext;
using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using HRM.Services.JobLeaveTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class UnifiedLeaveTypeTests
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Denied(Func<Task> action) { try { await action(); } catch (UnauthorizedAccessException) { return; } throw new Exception("Expected access denial"); }
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var actor = new UserContextDto { UserId = 1, IndividualId = 100, IsHRStaff = true };
        var permission = true;
        var access = Proxy.For<IUserAccessService>((m,a) => m switch {
            "RequireContextAsync" => Task.FromResult(actor), "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == 10), "HasPermissionAsync" => Task.FromResult(permission && (string)a[0]! == "Admin"), _ => throw new Exception(m)
        });
        var logs = Proxy.For<IOperationLogService>((m,a) => m == "CreateAsync" ? Audit((HrmTeContext)a[0]!) : throw new Exception(m));
        var day = new DateTime(2026,10,26);
        await using (var db = factory.CreateDbContext())
        {
            Check(db.Model.FindEntityType(typeof(LeaveType))!.GetTableName() == "LT_LeaveTypes" &&
                db.Model.FindEntityType(typeof(LeavePolicy))!.GetTableName() == "LT_LeavePolicies" &&
                db.Model.FindEntityType(typeof(LeavePolicyAccrualRule))!.GetTableName() == "LT_LeavePolicyAccrualRules" &&
                db.Model.FindEntityType(typeof(LeaveTypeMigrationAudit))!.GetTableName() == "LT_TypeMigrationAudits",
                "Leave configuration tables must share the established LT_ prefix");
            db.RequestTypes.AddRange(new HRM.Models.Archives.RequestType { RequestTypeId = 1, TypeName = "User Registration" },
                new HRM.Models.Archives.RequestType { RequestTypeId = 34, TypeName = "Annual Leave Request" });
            db.Jobs.AddRange(new Job { JobId = 1, OrganisationID = 10, IndividualID = 100, JobTypeId = 1, JobStateId = 4 },
                new Job { JobId = 2, OrganisationID = 20, IndividualID = 200, JobTypeId = 1, JobStateId = 4 });
            db.LeaveTypes.AddRange(new LeaveType { LeaveTypeId = 10, OrganisationId = 10, Code = "AL", Name = "Annual Leave", IncludeHolidays = false, RequestTypeId = 5, Duration = 30 },
                new LeaveType { LeaveTypeId = 20, OrganisationId = 20, Code = "OTHER", Name = "Foreign" },
                new LeaveType { LeaveTypeId = 30, OrganisationId = 20, Code = "SHARED", Name = "Shared", IsGlobal = true, Duration = 7 });
            db.LeavePolicies.AddRange(new LeavePolicy { LeavePolicyId = 1, OrganisationId = 10, LeaveTypeId = 10, EffectiveFrom = day, IsActive = true, IncludeHolidays = true, RequestTypeId = 99, IncludePay = true, PayPercentage = 50 },
                new LeavePolicy { LeavePolicyId = 2, OrganisationId = 20, LeaveTypeId = 10, EffectiveFrom = day, IsActive = true, IncludeHolidays = false, RequestTypeId = 88 });
            await db.SaveChangesAsync();
        }
        var catalogue = new LeaveTypeCatalogueService(factory, access, logs);
        Check((await catalogue.GetAvailableAsync()).Select(t => t.LeaveTypeId).Order().SequenceEqual([10,30]), "Unified catalogue leaked a tenant or hid a global type");
        Check(!(await catalogue.CreateAsync(new() { Code = "SHARED", Name = "Duplicate" })).Success, "Duplicate code was accepted");
        Check(!(await catalogue.UpdateAsync(new() { LeaveTypeId = 20, Code = "OTHER", Name = "Forged" })).Success, "Foreign type was edited");
        Check(!(await catalogue.UpdateAsync(new() { LeaveTypeId = 30, Code = "SHARED", Name = "Forged shared" })).Success, "Shared type was edited by ordinary HR");
        Check(!(await catalogue.CreateAsync(new() { Code = "BAD", Name = "No route" })).Success, "Active type without a request route was accepted");
        Check(!(await catalogue.CreateAsync(new() { Code = "BAD", Name = "Registration", RequestTypeId = 1 })).Success, "User registration was accepted for leave routing");
        Check(!(await catalogue.CreateAsync(new() { Code = "BAD", Name = "Missing", RequestTypeId = 999 })).Success, "Unknown request type was accepted");
        Check(!(await catalogue.CreateAsync(new() { Code = "BAD", Name = "Change", RequestTypeId = 105 })).Success, "Early return was accepted as a leave application route");
        Check((await catalogue.GetRequestTypesAsync()).Select(x => x.Id).SequenceEqual([34]), "Request selector included registration or change request types");
        var created = await catalogue.CreateAsync(new() { Code = "NEW", Name = "New type", RequestTypeId = 34 });
        Check(created.Success, created.Message);
        await using (var db = factory.CreateDbContext())
        {
            var row = await db.LeaveTypes.SingleAsync(t => t.LeaveTypeId == created.Data);
            Check(row.OperationLogId > 0 && row.Name == "New type", "Catalogue creation lost audit/name");
            Check(await db.LeaveTypeReasonTypes.AnyAsync(r => r.LeaveTypeId == row.LeaveTypeId), "New type has no selectable reason");
            var before = await LeavePolicyResolver.ResolveAsync(db, 10, 10, day.AddDays(-1));
            var current = await LeavePolicyResolver.ResolveAsync(db, 10, 10, day);
            Check(before?.IncludeHolidays == false && before.RequestTypeId == 5, "Pre-policy defaults were lost");
            Check(current?.IncludeHolidays == true && current.RequestTypeId == 99 && current.PayPercentage == 50, "Effective policy did not control application rules");
            Check((await db.LeaveTypes.SingleAsync(t => t.LeaveTypeId == 10)).IncludeHolidays == false, "Resolving a policy mutated catalogue defaults");
            Check(await LeavePolicyResolver.ResolveAsync(db,20,10,day) == null, "Policy resolver crossed tenants");
            db.LeavePolicies.Add(new LeavePolicy { OrganisationId = 10, LeaveTypeId = 10, EffectiveFrom = day, IsActive = true }); await db.SaveChangesAsync();
            try { await LeavePolicyResolver.ResolveAsync(db,10,10,day); throw new Exception("Ambiguous active policies were silently selected"); } catch (InvalidOperationException) { }
        }
        var policies = new LeavePolicyService(factory, access, Microsoft.Extensions.Logging.Abstractions.NullLogger<LeavePolicyService>.Instance, logs);
        Check(!(await policies.CreateAsync(new() { LeaveTypeId = 30, RequestTypeId = 0, EffectiveFrom = day })).Success, "Policy without a request type was accepted");
        Check(!(await policies.CreateAsync(new() { LeaveTypeId = 30, RequestTypeId = 1, EffectiveFrom = day })).Success, "Policy routed leave through User Registration");
        Check(!(await policies.UpdateAsync(new() { LeavePolicyId = 1, LeaveTypeId = 10, RequestTypeId = 999, EffectiveFrom = day })).Success, "Policy edit accepted an unknown request type");
        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 66, Code = "ANNUAL-LOCAL", Name = "Annual local", OrganisationId = 10, RequestTypeId = 34 });
            db.Requests.Add(new Request { RequestId = 500, RequestTypeId = 34, OrganisationBusinessEntityID = 10, ApplicantBusinessEntityId = 100, ApplicationDate = day });
            db.Leaves.Add(new Leaf { LeaveId = 500, JobId = 1, LeaveTypeId = 66, FromDate = day, ToDate = day, Duration = 1 });
            db.LeaveRequests.Add(new LeaveRequest { LeaveId = 500, RequestId = 500 });
            db.WfWorkflowDefinitions.Add(new HRM.Models.Workflow.WfWorkflowDefinition {
                WorkflowDefinitionId = 500, Name = "Annual review", RequestTypeId = 34, OrganisationBusinessEntityId = 10, LeaveRequestKind = 0,
                WorkflowJobTypes = [new() { JobTypeId = 1, IsActive = true }], Stages = [new() { StageOrder = 1, StageName = "HR", IsActive = true }]
            });
            await db.SaveChangesAsync();
            var context = await new HRM.Services.Interfaces.AppovalWF.WorkflowRequestContextResolver(factory).ResolveAsync(db,500,1);
            Check(context.LeaveTypeId == 66 && context.RequestTypeId == 34, "Independent leave and request type IDs were confused");
            var flow = await LeaveFlowConfigurationService.ResolveAsync(db,context);
            Check(flow.WorkflowDefinitionId == 500, "Annual leave did not use its linked request approval flow");
        }
        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypeReasonTypes.AddRange(new LeaveTypeReasonType { LeaveTypeReasonTypeId = 601, LeaveTypeId = 66, ReasonTypeName = "Other" },
                new() { LeaveTypeReasonTypeId = 602, LeaveTypeId = 66, ReasonTypeName = "Holiday" },
                new() { LeaveTypeReasonTypeId = 603, LeaveTypeId = 66, ReasonTypeName = "Family" },
                new() { LeaveTypeReasonTypeId = 604, LeaveTypeId = 66, ReasonTypeName = "Personal" },
                new() { LeaveTypeReasonTypeId = 101, LeaveTypeId = 10, ReasonTypeName = "Other" });
            db.JobLeaveTypes.Add(new JobLeaveType { JobLeaveTypeId = 660, JobId = 1, LeaveTypeId = 66, IsValid = true, IsLeaveInfoUpdated = true, RemainingDays = 30 });
            (await db.Requests.SingleAsync(x=>x.RequestId==500)).RequestStateId = 1;
            (await db.Leaves.SingleAsync(x=>x.LeaveId==500)).LeaveStateId = 1;
            db.LeaveReasons.Add(new LeaveReason { LeaveId = 500, LeaveTypeReasonTypeId = 101, Reason = "Saved explanation" });
            db.WfRequestInstances.Add(new() { RequestId = 500, WorkflowDefinitionId = 500, WorkflowStatus = "Returned" });
            await db.SaveChangesAsync();
        }
        actor.ActiveJob = new ActiveJobDto { JobId = 1, OrganisationId = 10 };
        var applications = new LeaveService(factory,null!,null!,null!,access,null!,null!);
        var selected = await applications.GetApplicationLeaveTypeAsync(660);
        Check(selected?.Reasons.Select(x=>x.Id).Order().SequenceEqual([601,602,603,604]) == true, "Staff application did not fetch canonical related reasons");
        var delegated = await applications.GetApplicationLeaveTypeAsync(660,1);
        Check(delegated?.Reasons.Select(x=>x.Id).Order().SequenceEqual([601,602,603,604]) == true, "HR application did not fetch the selected staff's related reasons");
        var changes = new LeaveChangeService(factory,access,null!,logs,null!,null!,null!);
        var returned = await changes.GetReturnedApplicationAsync(500);
        Check(returned.ReasonId == 601 && returned.Reasons.Count == 4, "Returned application did not preselect its canonical reason");
        await using(var db=factory.CreateDbContext()) Check((await db.LeaveReasons.SingleAsync(x=>x.LeaveId==500)).LeaveTypeReasonTypeId == 101, "Loading the edit form rewrote historical reason data");
        var assignments = new JobLeaveTypeService(factory, logs, access);
        Check((await assignments.AssignAsync(new() { JobId = 1, LeaveTypeId = 30, EffectiveFromDate = day })).Success, "Shared canonical type could not be assigned");
        Check(!(await assignments.AssignAsync(new() { JobId = 1, LeaveTypeId = 30, EffectiveFromDate = day })).Success, "Duplicate assignment was accepted");
        Check(!(await assignments.AssignAsync(new() { JobId = 2, LeaveTypeId = 30, EffectiveFromDate = day })).Success, "Foreign job was assigned");
        var disabled = await catalogue.UpdateAsync(new() { LeaveTypeId = created.Data, Code = "NEW", Name = "New type", IsActive = false });
        Check(disabled.Success && !(await catalogue.GetAvailableAsync()).Any(t=>t.LeaveTypeId == created.Data) && (await catalogue.GetManagementAsync()).Any(t=>t.LeaveTypeId == created.Data), "Inactive type cannot be managed or remains assignable");
        permission = false; await Denied(() => catalogue.CreateAsync(new() { Code = "DENY", Name = "Denied" }));
    }
    static async Task<OperationLog> Audit(HrmTeContext db) { var log = new OperationLog(); db.OperationLogs.Add(log); await db.SaveChangesAsync(); return log; }
}
