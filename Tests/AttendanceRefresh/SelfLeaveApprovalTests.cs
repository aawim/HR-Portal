using System.Reflection;
using HRM.Components.Shared;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;

static class SelfLeaveApprovalTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options;
        await using var db = new SelfLeaveContext(options);
        var user = new UserContextDto { UserId = 1, IndividualId = 10, BusinessEntityId = 10 };
        var access = Proxy.For<IUserAccessService>((name, _) => name == "HasOrganisationAccessAsync"
            ? Task.FromResult(true) : Task.FromResult(user));
        var service = new LeaveChangeService(null!, access, null!, null!, null!, null!, null!);
        var request = new Request { RequestId = 1, RequestTypeId = 48,
            RequestStateId = 2, OrganisationBusinessEntityID = 100 };
        var leave = new Leaf { LeaveId = 1, LeaveStateId = 2, Job = new Job { JobId = 1, IndividualID = 10 } };
        var instance = new WfRequestInstance { WorkflowRequestInstanceId = 1, RequestId = 1,
            WorkflowDefinitionId = 1, CurrentRequestStageInstanceId = 1, WorkflowStatus = "InProgress" };
        db.WfWorkflowStages.Add(new WfWorkflowStage { WorkflowStageId = 1, WorkflowDefinitionId = 1, StageOrder = 1 });
        db.WfRequestStageInstances.Add(new WfRequestStageInstance { RequestStageInstanceId = 1,
            WorkflowRequestInstanceId = 1, WorkflowStageId = 1 });
        db.WfRequestApprovals.Add(new WfRequestApproval { RequestApprovalId = 1, RequestStageInstanceId = 1, ApproverUserId = 1 });
        foreach (var id in new[] { 1, 2, 3, 4, 5 })
            db.RequestStates.Add(new RequestState { RequestStateId = id, StateName = $"State {id}", IsProcessingState = id is 2 or 3 });
        foreach (var pair in new[] { (2, 3), (2, 5), (3, 4), (3, 5), (1, 2), (1, 5) })
            db.RequestTypesAllowedRequestStateTransitions.Add(new RequestTypesAllowedRequestStateTransition
            { RequestTypeId = request.RequestTypeId, FromRequestStateId = pair.Item1, ToRequestStateId = pair.Item2 });
        await db.SaveChangesAsync();
        var method = typeof(LeaveChangeService).GetMethod("AllowedActionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        async Task<List<RequestActionDto>> Actions() =>
            await (Task<List<RequestActionDto>>)method.Invoke(service, [db, request, leave, instance, user])!;
        var actions = await Actions();
        Check(!actions.Any(x => x.ToStateId == 3) && actions.Any(x => x.ToStateId == 5), "Owner can verify, or lost cancellation");
        request.RequestStateId = 3;
        actions = await Actions();
        Check(!actions.Any(x => x.ToStateId == 4), "Owner can approve even when assigned as approver");
        request.RequestStateId = 1;
        instance.WorkflowStatus = "Returned";
        Check((await Actions()).Any(x => x.ToStateId == 2), "Owner lost correction/resubmission");
        leave.Job.IndividualID = 20;
        request.RequestStateId = 2;
        instance.WorkflowStatus = "InProgress";
        Check((await Actions()).Any(x => x.ToStateId == 3), "Assigned reviewer cannot verify another employee's leave");
        var finalState = await db.RequestStates.SingleAsync(x => x.RequestStateId == 2);
        finalState.IsFinalState = true;
        await db.SaveChangesAsync();
        Check((await Actions()).Count == 0, "IsFinalState did not override IsProcessingState for request actions");
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}

sealed class SelfLeaveContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
}
