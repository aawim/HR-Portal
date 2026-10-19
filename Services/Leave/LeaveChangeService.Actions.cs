using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public partial class LeaveChangeService
{
    private async Task PopulateActionsAsync(HrmTeContext db, List<LeaveChangeRequestDto> items, UserContextDto user)
    {
        foreach (var item in items)
        {
            var request = await db.Requests.AsNoTracking().SingleAsync(x => x.RequestId == item.RequestId);
            var change = await db.LeaveChangeRequests.AsNoTracking().Include(x => x.Leave).ThenInclude(x => x.Job)
                .SingleAsync(x => x.RequestId == item.RequestId);
            var instance = await db.WfRequestInstances.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == item.RequestId);
            if (instance != null) item.Actions = await AllowedActionsAsync(db, request, change.Leave, instance, user);
        }
    }

    private async Task<List<RequestActionDto>> AllowedActionsAsync(
        HrmTeContext db, Request request, Leaf leave, WfRequestInstance instance, UserContextDto user)
    {
        var result = new List<RequestActionDto>();
        if (instance.CompletedAt.HasValue || instance.WorkflowStatus is not ("InProgress" or "Returned")) return result;
        var stage = await db.WfRequestStageInstances.AsNoTracking().Include(x => x.Approvals).Include(x => x.WorkflowStage)
            .SingleOrDefaultAsync(x => x.RequestStageInstanceId == instance.CurrentRequestStageInstanceId);
        if (stage == null) return result;
        var owner = leave.Job.IndividualID == user.IndividualId;
        var assignment = stage.Approvals.FirstOrDefault(x => x.ApproverUserId == user.UserId && x.Decision == "Pending");
        var assigned = instance.WorkflowStatus == "InProgress" && stage.StageStatus == "Pending" && assignment != null;
        if (assigned && stage.WorkflowStage.ApprovalMode.Equals("Sequential", StringComparison.OrdinalIgnoreCase) &&
            stage.Approvals.Any(x => x.Decision == "Pending" && x.ApprovalSequence < assignment!.ApprovalSequence)) assigned = false;
        var organisationId = request.OrganisationBusinessEntityID ?? 0;
        assigned = assigned && await access.HasOrganisationAccessAsync(organisationId);
        if (!owner && !assigned) return result;

        var rules = await db.RequestTypesStatesRequiredRolesForProcessings.AsNoTracking()
            .Where(x => x.RequestTypeId == request.RequestTypeId && x.RequestStateId == request.RequestStateId).ToListAsync();
        var namedApprovers = await db.WfStageApprovers.AsNoTracking()
            .AnyAsync(x => x.WorkflowStageId == stage.WorkflowStageId && x.ApproverType == "USER");
        var roles = assigned ? await UserRolesAsync(db, user.UserId, organisationId) : new List<int>();
        var nextStage = await db.WfWorkflowStages.AsNoTracking()
            .Where(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.IsActive && x.StageOrder > stage.WorkflowStage.StageOrder)
            .OrderBy(x => x.StageOrder).FirstOrDefaultAsync();
        var transitions = await db.RequestTypesAllowedRequestStateTransitions.AsNoTracking().Include(x => x.ToRequestState)
            .Where(x => x.RequestTypeId == request.RequestTypeId && x.FromRequestStateId == request.RequestStateId).ToListAsync();
        var handoverReady = await LeaveHandoverService.ReadyAsync(db, leave.LeaveId);
        foreach (var transition in transitions)
        {
            var state = transition.ToRequestState;
            var applicantAction = owner && (state.RequestStateId == SharedConfig.RequestStates.CANCELLED ||
                (request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE && state.RequestStateId == SharedConfig.RequestStates.PENDING_VERIFICATION));
            var required = rules.Where(x => x.ToRequestStateId == null || x.ToRequestStateId == state.RequestStateId).ToList();
            var processorAction = assigned && (namedApprovers || rules.Count == 0 || required.Any(x => roles.Contains(x.RoleId)));
            if (!applicantAction && !processorAction) continue;
            var stopping = state.RequestStateId is SharedConfig.RequestStates.REJECTED or SharedConfig.RequestStates.CANCELLED or SharedConfig.RequestStates.INCOMPLETE;
            if (!stopping && !handoverReady && !applicantAction) continue;
            if (!stopping && nextStage != null && state.RequestStateId != nextStage.RequestStateId) continue;
            if (!stopping && state.RequestStateId != SharedConfig.RequestStates.APPROVED && !state.IsProcessingState) continue;
            result.Add(new()
            {
                FromStateId = request.RequestStateId, ToStateId = state.RequestStateId,
                Label = string.IsNullOrWhiteSpace(state.ButtonActionName) ? state.StateName.Trim() : state.ButtonActionName.Trim(),
                NextStatus = state.StateName.Trim(), CommentRequired = state.CommentRequired
            });
        }
        return result.OrderBy(x => x.ToStateId).ToList();
    }

    private static async Task<List<int>> UserRolesAsync(HrmTeContext db, int userId, int organisationId)
    {
        var direct = db.UserRoles.AsNoTracking().Where(x => x.UserId == userId && x.IsActive && x.UserOrganisation.BusinessEntityID == organisationId)
            .Select(x => x.RoleId);
        var groups = db.UserAssignedUserGroups.AsNoTracking().Where(x => x.UserId == userId && x.UserGroup.UserOrganisation.BusinessEntityID == organisationId)
            .SelectMany(x => x.UserGroup.UserGroupRoles.Where(r => r.IsActive).Select(r => r.RoleId));
        return await direct.Union(groups).Distinct().ToListAsync();
    }

    public async Task<ServiceResult> ExecuteActionAsync(int requestId, RequestActionSubmissionDto action)
        => await ExecuteRequestActionAsync(requestId, action, false);

    public async Task<ServiceResult> ExecuteLeaveActionAsync(int requestId, RequestActionSubmissionDto action)
        => await ExecuteRequestActionAsync(requestId, action, true);

    private async Task<ServiceResult> ExecuteRequestActionAsync(int requestId, RequestActionSubmissionDto action, bool application)
    {
        var user = await access.RequireContextAsync();
        if (action.Comments?.Length > 250) return ServiceResult.Fail("Comments must be 250 characters or fewer.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var request = await db.Requests.FromSqlInterpolated(
                $"SELECT * FROM dbo.Requests WITH (UPDLOCK, HOLDLOCK) WHERE RequestID = {requestId}").SingleOrDefaultAsync()
                ?? throw new InvalidOperationException("The request was not found.");
            if (!application && request.RequestTypeId != SharedConfig.RequestTypes.LEAVE_CHANGE_REQUEST)
                throw new InvalidOperationException("This is not a leave change request.");
            if (request.RequestStateId != action.FromStateId)
                throw new InvalidOperationException("The request status has changed. Refresh and choose an available action.");
            var change = application ? null : await db.LeaveChangeRequests.Include(x => x.Leave).ThenInclude(x => x.Job)
                .SingleAsync(x => x.RequestId == requestId);
            var applicationLink = application ? await db.LeaveRequests.Include(x => x.Leave).ThenInclude(x => x.Job)
                .SingleOrDefaultAsync(x => x.RequestId == requestId) : null;
            var subjectLeave = change?.Leave ?? applicationLink?.Leave
                ?? throw new InvalidOperationException("The leave application was not found.");
            var instance = await db.WfRequestInstances.SingleOrDefaultAsync(x => x.RequestId == requestId)
                ?? throw new InvalidOperationException("The approval workflow has not started.");
            var allowed = await AllowedActionsAsync(db, request, subjectLeave, instance, user);
            var selected = allowed.FirstOrDefault(x => x.ToStateId == action.ToStateId)
                ?? throw new UnauthorizedAccessException("This transition is not allowed for the current request status and your permissions.");
            if (selected.CommentRequired && string.IsNullOrWhiteSpace(action.Comments))
                throw new InvalidOperationException("A comment is required for this action.");
            var stage = await db.WfRequestStageInstances.Include(x => x.Approvals).Include(x => x.WorkflowStage)
                .SingleAsync(x => x.RequestStageInstanceId == instance.CurrentRequestStageInstanceId);
            var target = selected.ToStateId;
            var cancelled = target == SharedConfig.RequestStates.CANCELLED;
            var rejected = target == SharedConfig.RequestStates.REJECTED;
            var returned = target == SharedConfig.RequestStates.INCOMPLETE;
            var resubmitted = request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE;
            var advancing = !cancelled && !rejected && !returned;
            var now = DateTime.UtcNow;
            var complete = true;
            if (!cancelled && !resubmitted)
            {
                var decision = stage.Approvals.Single(x => x.ApproverUserId == user.UserId && x.Decision == "Pending");
                if (advancing && await db.WfRequestActions.AnyAsync(x => x.RequestStageInstanceId == stage.RequestStageInstanceId &&
                    x.ActionCode == "VOTE" && x.ToRequestStateId != target))
                    throw new InvalidOperationException("This stage already has votes for a different next status.");
                decision.Decision = rejected ? "Rejected" : returned ? "Returned" : "Approved";
                decision.Comments = action.Comments?.Trim();
                decision.DecisionAt = now;
                if (advancing)
                {
                    var approved = stage.Approvals.Count(x => x.Decision == "Approved");
                    complete = stage.WorkflowStage.ApprovalMode.ToUpperInvariant() switch
                    {
                        "ANY" => approved > 0,
                        "ALL" or "SEQUENTIAL" => stage.Approvals.All(x => x.Decision == "Approved"),
                        "QUORUM" when stage.WorkflowStage.MinimumApprovals is > 0 && stage.WorkflowStage.MinimumApprovals <= stage.Approvals.Count
                            => approved >= stage.WorkflowStage.MinimumApprovals.Value,
                        _ => throw new InvalidOperationException("The configured approval mode or quorum is invalid.")
                    };
                }
            }
            if (complete)
            {
                stage.StageStatus = cancelled ? "Cancelled" : rejected ? "Rejected" : returned ? "Returned" : "Completed";
                stage.CompletedAt = now;
                foreach (var pending in stage.Approvals.Where(x => x.Decision == "Pending"))
                {
                    pending.Decision = "Skipped"; pending.DecisionAt = now;
                }
                if (application)
                {
                    await ApplyApplicationStateAsync(db, subjectLeave, target);
                }
                else if (target == SharedConfig.RequestStates.APPROVED)
                {
                    var leave = await LockLeaveAsync(db, change!.LeaveId)
                        ?? throw new InvalidOperationException("The original leave was not found.");
                    await ApplyApprovedChangeAsync(db, leave, change);
                }
                if (target is SharedConfig.RequestStates.APPROVED or SharedConfig.RequestStates.REJECTED or SharedConfig.RequestStates.CANCELLED)
                {
                    instance.WorkflowStatus = cancelled ? "Cancelled" : rejected ? "Rejected" : "Approved";
                    instance.ExecutionStatus = "Succeeded";
                    instance.CompletedAt = now;
                }
                else if (returned)
                {
                    instance.WorkflowStatus = "Returned";
                    instance.ExecutionStatus = "Pending";
                }
                else
                {
                    await OpenProcessingStageAsync(db, request, instance, stage, target, subjectLeave.JobId, now);
                }
                request.RequestStateId = target;
                request.LastStateChangedByUserId = user.UserId;
                request.LastStateChangedDate = now;
                request.StateChangeRemarks = action.Comments?.Trim();
            }
            db.WfRequestActions.Add(new()
            {
                WorkflowRequestInstanceId = instance.WorkflowRequestInstanceId,
                RequestStageInstanceId = stage.RequestStageInstanceId, PerformedByUserId = user.UserId,
                ActionCode = complete ? "TRANSITION" : "VOTE", FromRequestStateId = action.FromStateId,
                ToRequestStateId = target, Comments = action.Comments?.Trim(), PerformedAt = now
            });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return ServiceResult.Ok(complete
                ? application ? $"Status changed to {selected.NextStatus}." : target == SharedConfig.RequestStates.APPROVED ? $"Status changed to {selected.NextStatus}. Leave dates and balance have been updated." : $"Status changed to {selected.NextStatus}. Leave dates and balance are unchanged."
                : $"Your decision for {selected.NextStatus} was recorded. Other stage approvals are still required.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            await transaction.RollbackAsync();
            return ServiceResult.Fail(ex.Message);
        }
    }

    private async Task OpenProcessingStageAsync(HrmTeContext db, Request request, WfRequestInstance instance,
        WfRequestStageInstance previous, int target, int jobId, DateTime now)
    {
        var template = await db.WfWorkflowStages.AsNoTracking()
            .Where(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.IsActive && x.RequestStateId == target)
            .OrderBy(x => x.StageOrder).FirstOrDefaultAsync() ?? previous.WorkflowStage;
        var roles = await db.RequestTypesStatesRequiredRolesForProcessings.AsNoTracking()
            .Where(x => x.RequestTypeId == request.RequestTypeId && x.RequestStateId == target)
            .Select(x => x.RoleId).Distinct().ToListAsync();
        List<WfRequestApproval> assignments;
        var namedApprovers = await db.WfStageApprovers.AsNoTracking()
            .AnyAsync(x => x.WorkflowStageId == template.WorkflowStageId && x.ApproverType == "USER");
        if (roles.Count > 0 && !namedApprovers)
        {
            var org = request.OrganisationBusinessEntityID ?? 0;
            var users = await db.Users.AsNoTracking().Where(x => x.IsActive &&
                (x.UserRoles.Any(r => r.IsActive && roles.Contains(r.RoleId) && r.UserOrganisation.BusinessEntityID == org) ||
                 x.UserAssignedUserGroups.Any(g => g.UserGroup.UserOrganisation.BusinessEntityID == org &&
                    g.UserGroup.UserGroupRoles.Any(r => r.IsActive && roles.Contains(r.RoleId)))))
                .Select(x => x.UserId).Distinct().ToListAsync();
            assignments = users.Select(x => new WfRequestApproval { ApproverUserId = x, AssignedAt = now }).ToList();
        }
        else
        {
            var context = await workflowContext.ResolveAsync(db, request.RequestId, jobId);
            var users = await approvers.ResolveAsync(template.WorkflowStageId, context);
            assignments = users.GroupBy(x => x.UserId).Select(x => x.OrderBy(y => y.SequenceNumber).First())
                .Select(x => new WfRequestApproval { ApproverUserId = x.UserId, StageApproverId = x.StageApproverId, ApprovalSequence = x.SequenceNumber, AssignedAt = now }).ToList();
        }
        if (assignments.Count == 0) throw new InvalidOperationException("No eligible processors are configured for the next status.");
        var attempt = await db.WfRequestStageInstances.Where(x => x.WorkflowRequestInstanceId == instance.WorkflowRequestInstanceId && x.WorkflowStageId == template.WorkflowStageId)
            .Select(x => (int?)x.AttemptNumber).MaxAsync() ?? 0;
        var next = new WfRequestStageInstance
        {
            WorkflowRequestInstanceId = instance.WorkflowRequestInstanceId, WorkflowStageId = template.WorkflowStageId,
            AttemptNumber = attempt + 1, StartedAt = now, Approvals = assignments
        };
        db.WfRequestStageInstances.Add(next);
        await db.SaveChangesAsync();
        instance.CurrentRequestStageInstanceId = next.RequestStageInstanceId;
        instance.WorkflowStatus = "InProgress";
        instance.ExecutionStatus = "Pending";
    }
}
