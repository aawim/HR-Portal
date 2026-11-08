using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Helpers;
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
            item.IsOwnRequest = change.Leave.Job.IndividualID == (user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId);
            var instance = await db.WfRequestInstances.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == item.RequestId);
            if (instance != null)
            {
                var availability = await GetActionAvailabilityAsync(db, request, change.Leave, instance, user);
                item.Actions = availability.Actions;
                item.ActionsMessage = availability.Message;
                item.CurrentStage = await db.WfRequestStageInstances.AsNoTracking()
                    .Where(x => x.RequestStageInstanceId == instance.CurrentRequestStageInstanceId)
                    .Select(x => x.WorkflowStage.StageName).SingleOrDefaultAsync();
                item.PendingApprovers = await (from a in db.WfRequestApprovals.AsNoTracking()
                    join u in db.Users.AsNoTracking() on a.ApproverUserId equals u.UserId
                    where a.RequestStageInstanceId == instance.CurrentRequestStageInstanceId && a.Decision == "Pending" &&
                        instance.CompletedAt == null && instance.WorkflowStatus == "InProgress"
                    orderby a.ApprovalSequence
                    select u.FullName ?? u.Username).ToListAsync();
            }
        }
    }

    private sealed class RequestActionAvailability
    {
        public List<RequestActionDto> Actions { get; set; } = new();
        public string? Message { get; set; }
    }

    private async Task<List<RequestActionDto>> AllowedActionsAsync(
        HrmTeContext db, Request request, Leaf leave, WfRequestInstance instance, UserContextDto user)
        => (await GetActionAvailabilityAsync(db, request, leave, instance, user)).Actions;

    private async Task<RequestActionAvailability> GetActionAvailabilityAsync(
        HrmTeContext db, Request request, Leaf leave, WfRequestInstance instance, UserContextDto user)
    {
        var result = new RequestActionAvailability();
        var currentState = await db.RequestStates.AsNoTracking().SingleOrDefaultAsync(x => x.RequestStateId == request.RequestStateId);
        if (currentState?.IsFinalState == true)
        {
            result.Message = "This request is final. No further processing actions are available.";
            return result;
        }
        if (instance.CompletedAt.HasValue || instance.WorkflowStatus is not ("InProgress" or "Returned") ||
            request.RequestStateId is SharedConfig.RequestStates.APPROVED or SharedConfig.RequestStates.CANCELLED or SharedConfig.RequestStates.REJECTED)
        {
            result.Message = "This request is closed. No further actions are available.";
            return result;
        }

        if (!LeaveChangeRequestTypes.IsChange(request.RequestTypeId) &&
            leave.LeaveStateId is SharedConfig.LeaveStates.APPROVED or SharedConfig.LeaveStates.REJECTED or SharedConfig.LeaveStates.CANCELLED)
        {
            result.Message = "This leave application is already closed. Contact HR if its request status is inconsistent.";
            return result;
        }
        var stage = await db.WfRequestStageInstances.AsNoTracking().Include(x => x.Approvals).Include(x => x.WorkflowStage)
            .SingleOrDefaultAsync(x => x.RequestStageInstanceId == instance.CurrentRequestStageInstanceId &&
                x.WorkflowRequestInstanceId == instance.WorkflowRequestInstanceId);
        if (stage == null)
        {
            result.Message = "No current approval stage is available. Contact HR.";
            return result;
        }

        var personId = user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId;
        var owner = personId > 0 && leave.Job.IndividualID == personId;
        var assignment = stage.Approvals.FirstOrDefault(x => x.ApproverUserId == user.UserId && x.Decision == "Pending");
        var assigned = !owner && instance.WorkflowStatus == "InProgress" && stage.StageStatus == "Pending" &&
            !stage.CompletedAt.HasValue && assignment != null;
        var waitingForTurn = assigned && stage.WorkflowStage.ApprovalMode.Equals("Sequential", StringComparison.OrdinalIgnoreCase) &&
            stage.Approvals.Any(x => x.Decision == "Pending" && x.ApprovalSequence < assignment!.ApprovalSequence);
        var organisationId = request.OrganisationBusinessEntityID ?? 0;
        assigned = assigned && !waitingForTurn && await access.HasOrganisationAccessAsync(organisationId);
        result.Message = waitingForTurn ? "An earlier approver must review this request before your turn." :
            assignment == null && stage.Approvals.Any(x => x.ApproverUserId == user.UserId && x.Decision != "Pending")
                ? "Your decision for this stage has already been recorded. Other approvers must complete their review."
                : "You are not assigned to process this request at its current stage.";
        if (!owner && !assigned) return result;

        var rules = await db.RequestTypesStatesRequiredRolesForProcessings.AsNoTracking()
            .Where(x => x.RequestTypeId == request.RequestTypeId && x.RequestStateId == request.RequestStateId).ToListAsync();
        var configuredLeaveFlow = await db.WfWorkflowDefinitions.AnyAsync(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.LeaveRequestKind != null);
        if (configuredLeaveFlow && assigned)
        {
            try
            {
                var context = await workflowContext.ResolveAsync(db, request.RequestId, leave.JobId);
                var eligible = await approvers.ResolveAsync(stage.WorkflowStageId, context);
                assigned = eligible.Any(x => x.UserId == user.UserId);
            }
            catch (InvalidOperationException) { assigned = false; }
            if (!assigned && !owner)
            {
                result.Message = "You no longer meet this stage's group or supervisor requirements.";
                return result;
            }
        }
        var resubmitState = configuredLeaveFlow
            ? await db.WfWorkflowStages.Where(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.IsActive)
                .OrderBy(x => x.StageOrder).Select(x => x.RequestStateId).FirstAsync()
            : SharedConfig.RequestStates.PENDING_VERIFICATION;
        var roles = assigned ? await UserRolesAsync(db, user.UserId, organisationId) : new List<int>();
        var nextStage = await db.WfWorkflowStages.AsNoTracking()
            .Where(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.IsActive && x.StageOrder > stage.WorkflowStage.StageOrder)
            .OrderBy(x => x.StageOrder).FirstOrDefaultAsync();
        // Standard leave processing must verify before it can approve.
        var nextStateId = configuredLeaveFlow ? nextStage?.RequestStateId ?? SharedConfig.RequestStates.APPROVED : request.RequestStateId switch
        {
            SharedConfig.RequestStates.PENDING_VERIFICATION => (int?)SharedConfig.RequestStates.PENDING_APPROVAL,
            SharedConfig.RequestStates.PENDING_APPROVAL => SharedConfig.RequestStates.APPROVED,
            _ => nextStage?.RequestStateId
        };
        var transitions = await db.RequestTypesAllowedRequestStateTransitions.AsNoTracking().Include(x => x.ToRequestState)
            .Where(x => x.RequestTypeId == request.RequestTypeId && x.FromRequestStateId == request.RequestStateId).ToListAsync();
        var votedTargets = await db.WfRequestActions.AsNoTracking()
            .Where(x => x.RequestStageInstanceId == stage.RequestStageInstanceId && x.ActionCode == "VOTE")
            .Select(x => x.ToRequestStateId).Distinct().ToListAsync();
        var handoverReady = await LeaveHandoverService.ReadyAsync(db, leave.LeaveId);
        var blockedByHandover = false;
        var blockedByRole = false;

        foreach (var transition in transitions)
        {
            var state = transition.ToRequestState;
            var applicantAction = owner && (state.RequestStateId == SharedConfig.RequestStates.CANCELLED ||
                (request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE && state.RequestStateId == resubmitState));
            var required = rules.Where(x => x.ToRequestStateId == null || x.ToRequestStateId == state.RequestStateId).ToList();
            // A named assignment grants a place in the workflow, not every processing permission.
            var processorAction = assigned && (configuredLeaveFlow || rules.Count == 0 || required.Any(x => roles.Contains(x.RoleId)));
            if (!applicantAction && !processorAction)
            {
                blockedByRole |= assigned;
                continue;
            }

            var stopping = state.RequestStateId is SharedConfig.RequestStates.REJECTED or SharedConfig.RequestStates.CANCELLED or SharedConfig.RequestStates.INCOMPLETE;
            if (!applicantAction && !stopping)
            {
                if (nextStateId.HasValue && state.RequestStateId != nextStateId.Value) continue;
                if (state.RequestStateId != SharedConfig.RequestStates.APPROVED && !state.IsProcessingState) continue;
                if (votedTargets.Any(x => x != state.RequestStateId)) continue;
                if (state.RequestStateId == SharedConfig.RequestStates.APPROVED && !handoverReady)
                {
                    blockedByHandover = true;
                    continue;
                }
            }

            result.Actions.Add(new()
            {
                FromStateId = request.RequestStateId, ToStateId = state.RequestStateId,
                Label = RequestActionLabel(request.RequestStateId, state),
                NextStatus = state.StateName.Trim(), CommentRequired = state.CommentRequired || state.RequestStateId == SharedConfig.RequestStates.INCOMPLETE
            });
        }

        result.Actions = result.Actions.OrderBy(x => x.ToStateId).ToList();
        if (transitions.Count == 0)
        {
            result.Message = "No actions are available right now. Please contact your organisation's HR team for assistance.";
            return result;
        }
        result.Message = blockedByHandover ? "Leave approval is available after the assigned employees and a team or department supervisor approve the work handover." :
            result.Actions.Count > 0 ? null :
            owner ? "You can view and change your leave, but cannot verify or approve your own request." :
            blockedByRole ? "Your roles do not permit the configured actions at this request status." :
            "No actions are configured for you at this request status.";
        return result;
    }

    private static string RequestActionLabel(int fromStateId, RequestState state)
    {
        if (fromStateId == SharedConfig.RequestStates.PENDING_VERIFICATION &&
            state.RequestStateId == SharedConfig.RequestStates.PENDING_APPROVAL) return "Verify";
        if (fromStateId == SharedConfig.RequestStates.PENDING_APPROVAL &&
            state.RequestStateId == SharedConfig.RequestStates.APPROVED) return "Approve";
        if (!string.IsNullOrWhiteSpace(state.ButtonActionName)) return state.ButtonActionName.Trim();
        return state.RequestStateId switch
        {
            SharedConfig.RequestStates.APPROVED => "Approve",
            SharedConfig.RequestStates.REJECTED => "Reject",
            SharedConfig.RequestStates.CANCELLED => "Cancel request",
            SharedConfig.RequestStates.INCOMPLETE => "Return for correction",
            SharedConfig.RequestStates.PENDING_VERIFICATION => "Submit for verification",
            SharedConfig.RequestStates.PENDING_APPROVAL when fromStateId is SharedConfig.RequestStates.PENDING_VERIFICATION
                or SharedConfig.RequestStates.PENDING_HR_VERIFICATION => "Verify",
            SharedConfig.RequestStates.PENDING_APPROVAL => "Send for approval",
            SharedConfig.RequestStates.PENDING_HOD_APPROVAL => "Send to head of department",
            SharedConfig.RequestStates.PENDING_HR_VERIFICATION => "Send for HR verification",
            SharedConfig.RequestStates.PENDING_HR_APPROVAL => "Send for HR approval",
            SharedConfig.RequestStates.PENDING_HR_HEAD_APPROVAL => "Send for HR head approval",
            SharedConfig.RequestStates.PENDING_SUPERVISOR_VERIFICATION => "Send for supervisor verification",
            SharedConfig.RequestStates.PENDING_SUPERVISOR_APPROVAL => "Send for supervisor approval",
            _ => $"Move to {state.StateName.Trim()}"
        };
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

    private async Task<ServiceResult> ExecuteRequestActionAsync(int requestId, RequestActionSubmissionDto action, bool application, ReturnedLeaveApplicationDto? revision = null)
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
            if (!application && !LeaveChangeRequestTypes.IsChange(request.RequestTypeId))
                throw new InvalidOperationException("This is not a leave change request.");
            if (request.RequestStateId != action.FromStateId)
                throw new InvalidOperationException("The request status has changed. Refresh and choose an available action.");
            var change = application ? null : await db.LeaveChangeRequests.Include(x => x.Leave).ThenInclude(x => x.Job)
                .SingleAsync(x => x.RequestId == requestId);
            var applicationLink = application ? await db.LeaveRequests.Include(x => x.Leave).ThenInclude(x => x.Job)
                .SingleOrDefaultAsync(x => x.RequestId == requestId) : null;
            var subjectLeave = change?.Leave ?? applicationLink?.Leave
                ?? throw new InvalidOperationException("The leave application was not found.");
            if (application)
            {
                subjectLeave = await LockLeaveAsync(db, subjectLeave.LeaveId)
                    ?? throw new InvalidOperationException("The leave application was not found.");
                await db.Entry(subjectLeave).ReloadAsync();
                await db.Entry(subjectLeave).Reference(x => x.Job).LoadAsync();
            }
            var instance = await db.WfRequestInstances.SingleOrDefaultAsync(x => x.RequestId == requestId)
                ?? throw new InvalidOperationException("The approval workflow has not started.");
            if (revision != null && request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE &&
                await db.WfWorkflowDefinitions.AnyAsync(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.LeaveRequestKind != null))
                action.ToStateId = await db.WfWorkflowStages.Where(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.IsActive)
                    .OrderBy(x => x.StageOrder).Select(x => x.RequestStateId).FirstAsync();
            try
            {
                await RequestTransitionPolicy.RequireAllowedAsync(db, request.RequestTypeId, request.RequestStateId, action.ToStateId);
            }
            catch (RequestTransitionConfigurationException ex)
            {
                var person = user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId;
                if (!await access.HasOrganisationAccessAsync(subjectLeave.Job.OrganisationID) ||
                    person != subjectLeave.Job.IndividualID && !await db.WfRequestApprovals.AnyAsync(x =>
                        x.RequestStageInstanceId == instance.CurrentRequestStageInstanceId && x.ApproverUserId == user.UserId && x.Decision == "Pending"))
                    throw new UnauthorizedAccessException("You do not have access to perform this action.");
                throw new WorkflowConfigurationException(new HRM.DTOs.ApprovalFlow.WorkflowRequestContextDto
                {
                    RequestId = requestId, RequestTypeId = request.RequestTypeId, RequestStateId = request.RequestStateId,
                    OrganisationBusinessEntityId = subjectLeave.Job.OrganisationID, JobTypeId = subjectLeave.Job.JobTypeId,
                    SubjectJobId = subjectLeave.JobId
                }, ex.Detail, ex.Message) { ConfigurationUrl = "/admin/settings/config/state-transitions" };
            }
            var allowed = await AllowedActionsAsync(db, request, subjectLeave, instance, user);
            var selected = allowed.FirstOrDefault(x => x.ToStateId == action.ToStateId)
                ?? throw new UnauthorizedAccessException("This transition is not allowed for the current request status and your permissions.");
            if (selected.CommentRequired && string.IsNullOrWhiteSpace(action.Comments))
                throw new InvalidOperationException("A comment is required for this action.");
            if (application && request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE)
            {
                if (revision == null) throw new InvalidOperationException("Open the correction form to edit and resubmit this application.");
                await ApplyReturnedApplicationEditsAsync(db, request, subjectLeave, revision, user);
            }
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
            if (ex is WorkflowConfigurationException configuration) await configuration.NotifyAsync(factory);
            return ServiceResult.Fail(ex.Message);
        }
    }

    private async Task OpenProcessingStageAsync(HrmTeContext db, Request request, WfRequestInstance instance,
        WfRequestStageInstance previous, int target, int jobId, DateTime now)
    {
        var template = await db.WfWorkflowStages.AsNoTracking()
            .Where(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.IsActive && x.RequestStateId == target)
            .OrderBy(x => x.StageOrder).FirstOrDefaultAsync();
        var processingStage = template ?? previous.WorkflowStage;
        var roles = await db.RequestTypesStatesRequiredRolesForProcessings.AsNoTracking()
            .Where(x => x.RequestTypeId == request.RequestTypeId && x.RequestStateId == target)
            .Select(x => x.RoleId).Distinct().ToListAsync();
        List<WfRequestApproval> assignments;
        var namedApprovers = template != null && await db.WfStageApprovers.AsNoTracking()
            .AnyAsync(x => x.WorkflowStageId == template.WorkflowStageId && x.ApproverType == "USER");
        var configuredLeaveFlow = await db.WfWorkflowDefinitions.AnyAsync(x => x.WorkflowDefinitionId == instance.WorkflowDefinitionId && x.LeaveRequestKind != null);
        if (roles.Count > 0 && !namedApprovers && !configuredLeaveFlow)
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
            var users = await approvers.ResolveAsync(processingStage.WorkflowStageId, context);
            assignments = users.GroupBy(x => x.UserId).Select(x => x.OrderBy(y => y.SequenceNumber).First())
                .Select(x => new WfRequestApproval { ApproverUserId = x.UserId, StageApproverId = x.StageApproverId, ApprovalSequence = x.SequenceNumber, AssignedAt = now }).ToList();
        }
        if (assignments.Count == 0) throw new InvalidOperationException("No eligible processors are configured for the next status.");
        var attempt = await db.WfRequestStageInstances.Where(x => x.WorkflowRequestInstanceId == instance.WorkflowRequestInstanceId && x.WorkflowStageId == processingStage.WorkflowStageId)
            .Select(x => (int?)x.AttemptNumber).MaxAsync() ?? 0;
        var next = new WfRequestStageInstance
        {
            WorkflowRequestInstanceId = instance.WorkflowRequestInstanceId, WorkflowStageId = processingStage.WorkflowStageId,
            AttemptNumber = attempt + 1, StartedAt = now, Approvals = assignments
        };
        db.WfRequestStageInstances.Add(next);
        await db.SaveChangesAsync();
        instance.CurrentRequestStageInstanceId = next.RequestStageInstanceId;
        instance.WorkflowStatus = "InProgress";
        instance.ExecutionStatus = "Pending";
    }
}
