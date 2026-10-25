using HRM.DTOs.ApprovalFlow;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record WorkflowRequestSubject(Request Request, int JobId, int IndividualId, int OrganisationId)
{
    public int RequestId => Request.RequestId;
}

// Shared sequential-stage processing. Callers own their tenant-scoped transaction and business validation.
public sealed class WorkflowRequestProcessing(IWorkflowApproverResolver resolver, IWorkflowRequestContextResolver contextResolver)
{
    public static Task<WfRequestInstance?> Instance(HrmTeContext db, int requestId) => db.WfRequestInstances
        .Include(i => i.WorkflowDefinition).ThenInclude(d => d.Stages)
        .Include(i => i.CurrentStage).ThenInclude(s => s!.WorkflowStage)
        .Include(i => i.CurrentStage).ThenInclude(s => s!.Approvals).SingleOrDefaultAsync(i => i.RequestId == requestId);
    public async Task<List<WorkflowApproverCandidate>> Candidates(HrmTeContext db, WorkflowRequestSubject request, int stage)
    {
        var context = await contextResolver.ResolveAsync(db, request.RequestId, request.JobId);
        var candidates = await resolver.ResolveAsync(stage, context);
        var ids = candidates.Select(c => c.UserId).ToList();
        var allowed = await db.Users.Where(u => ids.Contains(u.UserId) && u.BusinessEntityID != request.IndividualId && u.IsActive &&
            u.LocalUserStateId == 1 && u.UserOrganisation!.BusinessEntityID == request.OrganisationId).Select(u => u.UserId).ToListAsync();
        return candidates.Where(c => allowed.Contains(c.UserId)).DistinctBy(c => c.UserId).ToList();
    }
    public async Task<bool> CanApprove(HrmTeContext db, WorkflowRequestSubject request, WfRequestInstance? instance, int user, int person)
    {
        var stage = instance?.CurrentStage;
        if (person == request.IndividualId || instance?.WorkflowStatus != "InProgress" || instance.CompletedAt.HasValue ||
            request.Request.RequestState.IsFinalState || stage == null || stage.StageStatus != "Pending" || stage.CompletedAt.HasValue ||
            request.Request.RequestStateId != stage.WorkflowStage.RequestStateId) return false;
        var own = stage.Approvals.SingleOrDefault(a => a.ApproverUserId == user && a.Decision == "Pending");
        if (own == null || stage.WorkflowStage.ApprovalMode == "Sequential" && stage.Approvals.Any(a => a.Decision == "Pending" && a.ApprovalSequence < own.ApprovalSequence)) return false;
        try { return (await Candidates(db, request, stage.WorkflowStageId)).Any(c => c.UserId == user); }
        catch (InvalidOperationException) { return false; }
    }
    private static void Notify(HrmTeContext db, WorkflowRequestSubject request, int user, long stage) => db.RequestNotifications.Add(new()
    { RequestId = request.RequestId, UserId = user, NotificationKind = "ActionRequired", RequestStageInstanceId = stage,
        FromStateId = request.Request.RequestStateId, ToStateId = request.Request.RequestStateId, ReferenceNumber = request.Request.ReferenceNumber ?? "Request action required", CreatedAtUtc = DateTime.UtcNow });
    public async Task<int> ExecuteAsync(HrmTeContext db, WorkflowRequestSubject r, int expectedState, string action,
        string? comment, int user, int person, Func<Task> validateBeforeApproval, Func<Task> applyFinalApproval)
    {
        if (action is not ("Approve" or "Reject" or "Cancel")) throw new InvalidOperationException("Unsupported action.");
        if (comment?.Length > 2000 || action != "Approve" && string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("Enter an action comment of up to 2,000 characters.");
            var instance = await Instance(db, r.RequestId) ?? throw new InvalidOperationException("The request is no longer available for action.");
            var stage = instance.CurrentStage ?? throw new InvalidOperationException("Refresh the request before acting.");
            if (r.Request.RequestStateId != expectedState || r.Request.RequestState.IsFinalState || instance.WorkflowStatus != "InProgress" || instance.CompletedAt.HasValue)
                throw new InvalidOperationException("The request changed. Refresh before acting.");
            if (action == "Cancel" ? r.IndividualId != person : !await CanApprove(db, r, instance, user, person))
                throw new UnauthorizedAccessException("You are not assigned to perform this action. You cannot approve your own request.");
            var from = r.Request.RequestStateId; var to = action == "Reject" ? 6 : action == "Cancel" ? 5 : from;
            if (action == "Approve")
            {
                await validateBeforeApproval();
                var own = stage.Approvals.Single(a => a.ApproverUserId == user);
                own.Decision = "Approved"; own.Comments = comment; own.DecisionAt = DateTime.UtcNow;
                var count = stage.Approvals.Count(a => a.Decision == "Approved");
                var complete = stage.WorkflowStage.ApprovalMode switch { "Any" => count > 0, "All" or "Sequential" => count == stage.Approvals.Count,
                    "Quorum" => count >= (stage.WorkflowStage.MinimumApprovals ?? int.MaxValue), _ => throw new InvalidOperationException("Unsupported approval mode.") };
                if (complete)
                {
                    var next = instance.WorkflowDefinition.Stages.Where(s => s.IsActive && s.StageOrder > stage.WorkflowStage.StageOrder).OrderBy(s => s.StageOrder).FirstOrDefault();
                    to = next?.RequestStateId ?? 4;
                    await RequestTransitionPolicy.RequireAllowedAsync(db, r.Request.RequestTypeId, from, to);
                    stage.StageStatus = "Completed"; stage.CompletedAt = DateTime.UtcNow;
                    foreach (var a in stage.Approvals.Where(a => a.Decision == "Pending")) a.Decision = "Skipped";
                    if (next == null)
                    {
                        await applyFinalApproval();
                        instance.WorkflowStatus = "Approved"; instance.ExecutionStatus = "Succeeded"; instance.CompletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var candidates = await Candidates(db, r, next.WorkflowStageId);
                        if (candidates.Count == 0)
                            throw new WorkflowConfigurationException(await contextResolver.ResolveAsync(db, r.RequestId, r.JobId),
                                $"No eligible independent approvers remain for {next.StageName}.");
                        var opened = new WfRequestStageInstance { WorkflowRequestInstance = instance, WorkflowStage = next, StartedAt = DateTime.UtcNow };
                        foreach (var c in candidates) opened.Approvals.Add(new() { ApproverUserId = c.UserId, StageApproverId = c.StageApproverId, ApprovalSequence = c.SequenceNumber, AssignedAt = DateTime.UtcNow });
                        db.WfRequestStageInstances.Add(opened); await db.SaveChangesAsync(); instance.CurrentRequestStageInstanceId = opened.RequestStageInstanceId;
                        foreach (var c in candidates.Where(c => next.ApprovalMode != "Sequential" || c.SequenceNumber == candidates.Min(x => x.SequenceNumber))) Notify(db, r, c.UserId, opened.RequestStageInstanceId);
                    }
                }
                else if (stage.WorkflowStage.ApprovalMode == "Sequential")
                {
                    var pending = stage.Approvals.Where(a => a.Decision == "Pending").ToList();
                    foreach (var a in pending.Where(a => a.ApprovalSequence == pending.Min(p => p.ApprovalSequence))) Notify(db, r, a.ApproverUserId, stage.RequestStageInstanceId);
                }
            }
            else
            {
                await RequestTransitionPolicy.RequireAllowedAsync(db, r.Request.RequestTypeId, from, to);
                stage.StageStatus = action == "Reject" ? "Rejected" : "Cancelled"; stage.CompletedAt = DateTime.UtcNow;
                instance.WorkflowStatus = stage.StageStatus; instance.CompletedAt = DateTime.UtcNow;
                foreach (var a in stage.Approvals.Where(a => a.Decision == "Pending")) { a.Decision = action == "Reject" && a.ApproverUserId == user ? "Rejected" : "Skipped"; a.DecisionAt = DateTime.UtcNow; a.Comments = comment; }
            }
            if (await db.RequestStates.AnyAsync(s => s.RequestStateId == to && s.CommentRequired) && string.IsNullOrWhiteSpace(comment)) throw new InvalidOperationException("Enter a comment for this state change.");
            r.Request.RequestStateId = to; r.Request.LastStateChangedByUserId = user; r.Request.LastStateChangedDate = DateTime.UtcNow; r.Request.StateChangeRemarks = comment;
            db.WfRequestActions.Add(new() { WorkflowRequestInstance = instance, RequestStageInstance = stage, PerformedByUserId = user,
                ActionCode = action, FromRequestStateId = from, ToRequestStateId = to, Comments = comment, PerformedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return to;
    }
}
