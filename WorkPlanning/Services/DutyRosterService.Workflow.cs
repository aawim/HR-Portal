using System.Data;
using HRM.DTOs.WorkPlanning;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

public sealed partial class DutyRosterService
{
    private async Task<List<WorkflowApproverCandidate>> EligibleAsync(HrmTeContext db, DutyChangeRequest c, int stageId)
    {
        var context = await contexts.ResolveAsync(db, c.RequestId, c.SourceJobId);
        var candidates = await approvers.ResolveAsync(stageId, context);
        var active = await db.Users.Where(x => x.IsActive && x.LocalUserStateId == 1 && x.UserOrganisation!.BusinessEntityID == c.OrganisationId)
            .Select(x => x.UserId).ToListAsync();
        var excluded = await db.Users.Where(x => x.BusinessEntityID == c.SourceIndividualId || x.BusinessEntityID == c.TargetIndividualId)
            .Select(x => x.UserId).ToListAsync();
        excluded.Add(c.CreatedByUserId);
        return candidates.Where(x => active.Contains(x.UserId) && !excluded.Contains(x.UserId)).DistinctBy(x => x.UserId).ToList();
    }

    private async Task<WfWorkflowDefinition> DefinitionAsync(HrmTeContext db, DutyChangeRequest c)
    {
        var jobType = await db.Jobs.Where(x => x.JobId == c.SourceJobId).Select(x => x.JobTypeId).SingleAsync();
        var today = DateOnly.FromDateTime(c.SourceStart);
        var definition = await db.WfWorkflowDefinitions.Include(x => x.Stages).ThenInclude(x => x.Approvers)
            .Include(x => x.WorkflowJobTypes).Where(x => x.IsActive && x.RequestTypeId == c.Request.RequestTypeId && x.LeaveTypeId == null &&
                (x.LeaveRequestKind == null || x.LeaveRequestKind == 0) && (x.OrganisationBusinessEntityId == c.OrganisationId || x.OrganisationBusinessEntityId == null) &&
                (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= today) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= today) &&
                (!x.WorkflowJobTypes.Any() || x.WorkflowJobTypes.Any(j => j.JobTypeId == jobType && j.IsActive)))
            .OrderByDescending(x => x.OrganisationBusinessEntityId.HasValue).ThenByDescending(x => x.WorkflowJobTypes.Any())
            .ThenByDescending(x => x.VersionNumber).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Configure this duty Request Type for the staff member's JobType in Leave Management → Request Flow.");
        var stages = definition.Stages.Where(x => x.IsActive).OrderBy(x => x.StageOrder).ToList();
        if (stages.Count == 0) throw new InvalidOperationException("Automatic approval is reserved for later. Configure at least one approval stage.");
        if (stages.Select(x => x.RequestStateId).Distinct().Count() != stages.Count || stages.Any(x => x.RequestStateId == 10))
            throw new InvalidOperationException("Approval stages must use distinct processing states, separate from staff acceptance.");
        foreach (var stage in stages)
        {
            if (!await db.RequestStates.AnyAsync(x => x.RequestStateId == stage.RequestStateId && x.IsProcessingState && !x.IsFinalState))
                throw new InvalidOperationException("Each duty approval stage must use a processing state.");
            var eligible = await EligibleAsync(db, c, stage.WorkflowStageId);
            if (eligible.Count == 0 || (stage.ApprovalMode == "Quorum" && (stage.MinimumApprovals is null or < 1 || stage.MinimumApprovals > eligible.Count)))
                throw new InvalidOperationException($"No sufficient independent approvers for {stage.StageName}. Check the supervisor and UserGroup configuration.");
        }
        var route = new[] { 10 }.Concat(stages.Select(x => x.RequestStateId)).Append(4).ToArray();
        foreach (var pair in route.Zip(route.Skip(1))) await RequestTransitionPolicy.RequireAllowedAsync(db, c.Request.RequestTypeId, pair.First, pair.Second);
        foreach (var state in route.Where(x => x != 4))
            foreach (var final in new[] { 5, 6 }) await RequestTransitionPolicy.RequireAllowedAsync(db, c.Request.RequestTypeId, state, final);
        return definition;
    }

    private async Task OpenStageAsync(HrmTeContext db, DutyChangeRequest c, WfRequestInstance instance, WfWorkflowStage stage)
    {
        var candidates = await EligibleAsync(db, c, stage.WorkflowStageId);
        if (candidates.Count == 0) throw new InvalidOperationException($"No eligible approver for {stage.StageName}.");
        var current = new WfRequestStageInstance { WorkflowRequestInstance = instance, WorkflowStage = stage, StartedAt = DateTime.UtcNow };
        db.WfRequestStageInstances.Add(current);
        foreach (var candidate in candidates)
            current.Approvals.Add(new WfRequestApproval { ApproverUserId = candidate.UserId, StageApproverId = candidate.StageApproverId,
                ApprovalSequence = candidate.SequenceNumber, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        instance.CurrentRequestStageInstanceId = current.RequestStageInstanceId;
        var actionable = stage.ApprovalMode == "Sequential" ? candidates.Where(x => x.SequenceNumber == candidates.Min(a => a.SequenceNumber)) : candidates;
        foreach(var candidate in actionable)
            db.RequestNotifications.Add(new RequestNotification { RequestId = c.RequestId, UserId = candidate.UserId,
                NotificationKind = "ActionRequired", RequestStageInstanceId = current.RequestStageInstanceId, FromStateId = c.Request.RequestStateId,
                ToStateId = stage.RequestStateId, ReferenceNumber = c.Request.ReferenceNumber ?? $"DUTY-{c.RequestId}", CreatedAtUtc = DateTime.UtcNow });
    }

    private static async Task NotifyAsync(HrmTeContext db, DutyChangeRequest c, int[] people, string kind, int from, int to)
    {
        var users = await db.Users.Where(x => (people.Contains(x.BusinessEntityID) || (kind != "DutyAcceptance" && x.UserId == c.CreatedByUserId)) && x.IsActive && x.LocalUserStateId == 1 &&
            x.UserOrganisation!.BusinessEntityID == c.OrganisationId).Select(x => x.UserId).ToListAsync();
        foreach (var user in users.Distinct()) db.RequestNotifications.Add(new RequestNotification { UserId = user, RequestId = c.RequestId,
            ReferenceNumber = c.Request.ReferenceNumber ?? $"DUTY-{c.RequestId}", NotificationKind = kind,
            FromStateId = from, ToStateId = to, CreatedAtUtc = DateTime.UtcNow });
    }

    private async Task<bool> CanApproveAsync(HrmTeContext db, DutyChangeRequest c, WfRequestStageInstance? stage, int userId)
    {
        if (!c.IsOpen || c.Acceptance != "Accepted" || !c.SourceAcceptedAtUtc.HasValue || !c.AcceptedAtUtc.HasValue || stage == null || stage.StageStatus != "Pending" || stage.CompletedAt.HasValue) return false;
        var own = stage.Approvals.SingleOrDefault(x => x.ApproverUserId == userId && x.Decision == "Pending");
        if (own == null || (stage.WorkflowStage.ApprovalMode == "Sequential" && stage.Approvals.Any(x => x.Decision == "Pending" && x.ApprovalSequence < own.ApprovalSequence))) return false;
        try { return (await EligibleAsync(db, c, stage.WorkflowStageId)).Any(x => x.UserId == userId); }
        catch(InvalidOperationException) { return false; }
    }

    private static async Task RequireCommentAsync(HrmTeContext db, int to, string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment) && await db.RequestStates.AnyAsync(x => x.RequestStateId == to && x.CommentRequired))
            throw new InvalidOperationException("Enter an action comment for this status change.");
    }

    public async Task<List<DutyRequestRow>> GetRequestsAsync()
    {
        var actor = await access.RequireContextAsync(); var org = await WorkPlanningAccess.RequireOrganisationAsync(access);
        await using var db = await factory.CreateDbContextAsync();
        var ids = await db.WfRequestApprovals.Where(x => x.ApproverUserId == actor.UserId)
            .Select(x => x.RequestStageInstance.WorkflowRequestInstance.RequestId).ToListAsync();
        var changes = await db.DutyChangeRequests.Include(x => x.Request).ThenInclude(x => x.RequestState)
            .Where(x => x.OrganisationId == org && (x.SourceIndividualId == actor.IndividualId || x.TargetIndividualId == actor.IndividualId ||
                x.CreatedByUserId == actor.UserId || ids.Contains(x.RequestId))).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync();
        var result = new List<DutyRequestRow>();
        foreach (var c in changes)
        {
            var instance = await db.WfRequestInstances.Include(x => x.CurrentStage).ThenInclude(x => x!.WorkflowStage)
                .Include(x => x.CurrentStage).ThenInclude(x => x!.Approvals).SingleOrDefaultAsync(x => x.RequestId == c.RequestId);
            var source = await db.Individuals.SingleAsync(x => x.BusinessEntityId == c.SourceIndividualId);
            var target = await db.Individuals.SingleAsync(x => x.BusinessEntityId == c.TargetIndividualId);
            result.Add(new DutyRequestRow { Id = c.DutyChangeRequestId, RequestId = c.RequestId, StateId = c.Request.RequestStateId,
                Reference = c.Request.ReferenceNumber ?? $"DUTY-{c.RequestId}", Kind = c.Kind, Source = Name(source), Target = Name(target),
                Start = c.SourceStart, End = c.SourceEnd, SwapStart = c.SwapStart, SwapEnd = c.SwapEnd, Acceptance = c.Acceptance,
                SourceAcceptance = c.SourceAcceptedAtUtc.HasValue ? "Accepted" : "Pending",
                TargetAcceptance = c.AcceptedAtUtc.HasValue ? "Accepted" : "Pending",
                Status = c.Request.RequestState.IsFinalState ? c.Request.RequestState.StateName : $"Processing · {c.Request.RequestState.StateName}",
                Reason = c.Reason, Stage = instance?.CurrentStage?.WorkflowStage.StageName,
                CanAccept = c.IsOpen && c.Acceptance == "Pending" && c.Request.RequestStateId == 10 &&
                    ((c.SourceIndividualId == actor.IndividualId && !c.SourceAcceptedAtUtc.HasValue) || (c.TargetIndividualId == actor.IndividualId && !c.AcceptedAtUtc.HasValue)) &&
                    c.SourceStart > Now && (!c.SwapStart.HasValue || c.SwapStart > Now),
                CanApprove = await CanApproveAsync(db, c, instance?.CurrentStage, actor.UserId),
                CanCancel = c.IsOpen && (c.CreatedByUserId == actor.UserId || c.SourceIndividualId == actor.IndividualId) });
        }
        return result;
    }

    public async Task<HRM.DTOs.ServiceResult> ActAsync(long id, int expectedState, string action, string? comment = null)
    {
        var actor = await access.RequireContextAsync(); var org = await WorkPlanningAccess.RequireOrganisationAsync(access);
        if ((comment?.Length ?? 0) > 2000) return HRM.DTOs.ServiceResult.Failed("Comments must be under 2,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        if (action is "Accept" or "Approve")
        {
            var preview = await db.DutyChangeRequests.AsNoTracking().SingleOrDefaultAsync(x => x.DutyChangeRequestId == id && x.OrganisationId == org && x.IsOpen &&
                (x.SourceIndividualId == actor.IndividualId || x.TargetIndividualId == actor.IndividualId || x.CreatedByUserId == actor.UserId ||
                    db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.UserId && a.RequestStageInstance.WorkflowRequestInstance.RequestId == x.RequestId)));
            if (preview == null) return HRM.DTOs.ServiceResult.Failed("This request is not assigned to you in the current organisation.");
            try
            {
                await PreparePlansAsync(preview.TargetJobId, preview.TargetIndividualId, org, preview.SourceStart, preview.SourceEnd);
                if (preview.SwapStart.HasValue) await PreparePlansAsync(preview.SourceJobId, preview.SourceIndividualId, org, preview.SwapStart.Value, preview.SwapEnd!.Value);
            }
            catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException) { return HRM.DTOs.ServiceResult.Failed(ex.Message); }
        }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var query = db.Database.IsSqlServer() ? db.DutyChangeRequests.FromSqlInterpolated($"SELECT * FROM dbo.DC_DutyChangeRequests WITH (UPDLOCK,HOLDLOCK) WHERE DutyChangeRequestId={id}") : db.DutyChangeRequests.Where(x => x.DutyChangeRequestId == id);
            var c = await query.Include(x => x.Request).SingleOrDefaultAsync(x => x.OrganisationId == org) ?? throw new UnauthorizedAccessException("Request not found in this organisation.");
            if (!c.IsOpen || c.Request.RequestStateId != expectedState) throw new InvalidOperationException("The request changed. Refresh before acting.");
            var from = c.Request.RequestStateId; var to = from;
            var instance = await db.WfRequestInstances.Include(x => x.WorkflowDefinition).ThenInclude(x => x.Stages)
                .Include(x => x.CurrentStage).ThenInclude(x => x!.WorkflowStage).Include(x => x.CurrentStage).ThenInclude(x => x!.Approvals)
                .SingleOrDefaultAsync(x => x.RequestId == c.RequestId);
            var stage = instance?.CurrentStage;
            if (action is "Accept" or "Decline")
            {
                var sourceStaff = c.SourceIndividualId == actor.IndividualId;
                var targetStaff = c.TargetIndividualId == actor.IndividualId;
                if (from != 10 || c.Acceptance != "Pending" || (!sourceStaff && !targetStaff)) throw new UnauthorizedAccessException("Only the two participating staff members can accept or decline.");
                if (action == "Accept" && (sourceStaff ? c.SourceAcceptedAtUtc.HasValue : c.AcceptedAtUtc.HasValue))
                    throw new InvalidOperationException("You have already accepted this duty change. The other staff member must accept independently.");
                if (action == "Decline") { to = 6; c.Acceptance = "Declined"; }
                else
                {
                    var source = await LockAssignmentAsync(db, c.SourceAssignmentId);
                    var swap = c.SwapAssignmentId.HasValue ? await LockAssignmentAsync(db, c.SwapAssignmentId.Value) : null;
                    await ValidatePairAsync(db, c, source, swap);
                    var bothAccepted = (sourceStaff || c.SourceAcceptedAtUtc.HasValue) && (targetStaff || c.AcceptedAtUtc.HasValue);
                    if (bothAccepted)
                    {
                        var definition = instance?.WorkflowDefinition ?? throw new InvalidOperationException("The saved workflow is unavailable. Cancel and submit a new request.");
                        var first = definition.Stages.Where(x => x.IsActive).OrderBy(x => x.StageOrder).First();
                        to = first.RequestStateId;
                        await RequestTransitionPolicy.RequireAllowedAsync(db, c.Request.RequestTypeId, from, to);
                        await RequireCommentAsync(db, to, comment);
                        instance!.WorkflowStatus = "InProgress";
                        await OpenStageAsync(db, c, instance, first);
                        c.Acceptance = "Accepted";
                    }
                    else await RequireCommentAsync(db, from, comment);
                    if (sourceStaff) { c.SourceAcceptedByUserId = actor.UserId; c.SourceAcceptedAtUtc = DateTime.UtcNow; }
                    else { c.AcceptedByUserId = actor.UserId; c.AcceptedAtUtc = DateTime.UtcNow; }
                }
            }
            else if (action == "Cancel")
            {
                if (c.CreatedByUserId != actor.UserId && c.SourceIndividualId != actor.IndividualId) throw new UnauthorizedAccessException("Only the requesting staff member can cancel.");
                to = 5;
            }
            else if (action is "Approve" or "Reject")
            {
                if (!await CanApproveAsync(db, c, stage, actor.UserId)) throw new UnauthorizedAccessException("You are not the current eligible approver for this request.");
                if (action == "Reject") to = 6;
                else
                {
                    var vote = stage!.Approvals.Single(x => x.ApproverUserId == actor.UserId && x.Decision == "Pending");
                    vote.Decision = "Approved"; vote.DecisionAt = DateTime.UtcNow; vote.Comments = comment;
                    var count = stage.Approvals.Count(x => x.Decision == "Approved");
                    var complete = stage.WorkflowStage.ApprovalMode switch { "Any" => count > 0, "Quorum" => count >= (stage.WorkflowStage.MinimumApprovals ?? int.MaxValue), _ => stage.Approvals.All(x => x.Decision == "Approved") };
                    if (complete)
                    {
                        var next = instance!.WorkflowDefinition.Stages.Where(x => x.IsActive && x.StageOrder > stage.WorkflowStage.StageOrder).OrderBy(x => x.StageOrder).FirstOrDefault();
                        to = next?.RequestStateId ?? 4;
                        await RequestTransitionPolicy.RequireAllowedAsync(db, c.Request.RequestTypeId, from, to);
                        await RequireCommentAsync(db, to, comment);
                        if (next != null) await OpenStageAsync(db, c, instance, next);
                        else await ApplyMovementAsync(db, c, actor.UserId);
                        stage.StageStatus = "Completed"; stage.CompletedAt = DateTime.UtcNow;
                        foreach (var pending in stage.Approvals.Where(x => x.Decision == "Pending")) pending.Decision = "Skipped";
                    }
                }
            }
            else throw new InvalidOperationException("Unknown duty request action.");
            if (to != from) await RequestTransitionPolicy.RequireAllowedAsync(db, c.Request.RequestTypeId, from, to);
            if (to != from) await RequireCommentAsync(db, to, comment);
            if (to is 4 or 5 or 6)
            {
                c.IsOpen = false;
                if (instance != null)
                {
                    instance.WorkflowStatus = to == 4 ? "Approved" : to == 5 ? "Cancelled" : "Rejected";
                    instance.ExecutionStatus = to == 4 ? "Succeeded" : "NotStarted"; instance.CompletedAt = DateTime.UtcNow;
                    if (stage != null && stage.CompletedAt == null) { stage.StageStatus = instance.WorkflowStatus; stage.CompletedAt = DateTime.UtcNow;
                        foreach (var pending in stage.Approvals.Where(x => x.Decision == "Pending")) pending.Decision = "Skipped"; }
                }
            }
            c.Request.RequestStateId = to; c.Request.LastStateChangedDate = DateTime.UtcNow; c.Request.LastStateChangedByUserId = actor.UserId;
            c.Request.StateChangeRemarks = comment;
            if (instance != null) db.WfRequestActions.Add(new WfRequestAction { WorkflowRequestInstance = instance, PerformedByUserId = actor.UserId,
                ActionCode = action.ToUpperInvariant(), FromRequestStateId = from, ToRequestStateId = to, Comments = comment, PerformedAt = DateTime.UtcNow });
            var notificationKind = action switch { "Accept" when to != from => "DutyReadyForApproval", "Accept" => "DutyAccepted", "Decline" => "DutyDeclined", "Cancel" => "DutyCancelled", "Reject" => "DutyRejected", "Approve" when to == 4 => "DutyApproved", _ => "DutyVote" };
            await NotifyAsync(db, c, [c.SourceIndividualId, c.TargetIndividualId], notificationKind, from, to);
            await db.SaveChangesAsync(); await tx.CommitAsync(); return HRM.DTOs.ServiceResult.Ok($"Duty request: {action} recorded.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or DbUpdateException)
        { await tx.RollbackAsync(); return HRM.DTOs.ServiceResult.Failed(ex is DbUpdateException ? "The request changed while saving. Refresh and try again." : ex.Message); }
    }
}
