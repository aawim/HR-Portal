using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Attendance;
using HRM.DTOs.ApprovalFlow;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record LateCheckInRow(int Id, int StateId, string Reference, string Staff, string Schedule, DateTime ScheduledStart,
    DateTime ActualCheckIn, int LateMinutes, string Reason, string Status, string Stage, bool CanApprove, bool CanCancel, List<string> History);

public sealed class LateCheckInRequestService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IAttendanceService attendance, IRequestService requests, IOperationLogService logs, IWorkflowEngine workflow,
    IWorkflowApproverResolver resolver, IWorkflowRequestContextResolver contextResolver)
{
    public const string TypeName = "Late Check In Request";
    public const string PageUrl = "/online/attendance/attendance-request/late-check-in";
    private static DateTime Today => DateTime.UtcNow.AddHours(5).Date;
    private async Task<(int User, int Person, int Org, int Job)> Scope()
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select your current organisation.");
        return (actor.UserId, actor.IndividualId, org, actor.ActiveJob?.JobId ?? 0);
    }
    public async Task<List<StaffScheduleAttendanceDto>> GetCandidatesAsync(DateTime from, DateTime to)
    {
        var actor = await Scope();
        var facts = await attendance.GetStaffScheduleAttendanceAsync(actor.Person, from, to);
        await using var db = await factory.CreateDbContextAsync();
        var used = await db.LateCheckInRequests.Where(r => r.OrganisationId == actor.Org && r.IndividualId == actor.Person &&
            r.Request.RequestStateId != 5 && r.Request.RequestStateId != 6).Select(r => new { r.AttendanceLogId, r.WorkAssignmentId }).ToListAsync();
        return facts.Where(f => f.RequiresAttendance && f.CheckInLogId.HasValue && f.CheckIn > f.ScheduledStart && f.CheckIn <= DateTime.UtcNow.AddHours(5) && f.Date <= Today &&
            !used.Any(r => r.AttendanceLogId == f.CheckInLogId && r.WorkAssignmentId == f.WorkAssignmentId)).ToList();
    }
    public async Task<ServiceResult> SubmitAsync(long assignment, int logId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) return ServiceResult.Fail("Enter a reason of up to 1,000 characters.");
        var actor = await Scope();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var log = await db.AttendanceLogs.AsNoTracking().SingleOrDefaultAsync(l => l.AttendanceLogId == logId &&
                l.IndividualId == actor.Person && l.OrganisationId == actor.Org && l.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid)
                ?? throw new InvalidOperationException("The check-in event is no longer available. Refresh the list.");
            var facts = await AttendanceScheduleFacts.LoadAsync(db, actor.Person, log.Date.Date.AddDays(-1), log.Date.Date, actor.Org);
            var fact = facts.SingleOrDefault(f => f.WorkAssignmentId == assignment && f.CheckInLogId == logId && f.RequiresAttendance &&
                f.CheckIn > f.ScheduledStart && f.CheckIn <= DateTime.UtcNow.AddHours(5) && f.Date <= Today) ?? throw new InvalidOperationException("This event is not a late check-in for your current schedule.");
            var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(j => j.JobId == actor.Job && j.IndividualID == actor.Person && j.OrganisationID == actor.Org &&
                j.JobStateId == 4 && j.JoinedDate <= fact.Date && (j.TerminatedDate == null || j.TerminatedDate > Today))
                ?? throw new InvalidOperationException("Active approved employment is required for this request.");
            var date = fact.Date;
            if (await db.Leaves.AnyAsync(l => l.Job.IndividualID == actor.Person && l.Job.OrganisationID == actor.Org && l.LeaveStateId == 4 && l.FromDate < date.AddDays(1) && l.ToDate >= date))
                throw new InvalidOperationException("This date is covered by approved leave. Please contact HR to reconcile it first.");
            if (await db.LateCheckInRequests.AnyAsync(r => r.AttendanceLogId == logId && r.WorkAssignmentId == assignment && r.Request.RequestStateId != 5 && r.Request.RequestStateId != 6))
                throw new InvalidOperationException("A pending or approved explanation already exists for this check-in.");
            var type = await db.RequestTypes.SingleAsync(t => t.TypeName == TypeName);
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.ATTENDANCE_LOG_CREATE, "Late check-in explanation submitted; clock event unchanged.");
            var request = await requests.CreateAsync(db, new() { RequestTypeId = type.RequestTypeId, ApplicantBusinessEntityId = actor.Person,
                OrganisationBusinessEntityId = actor.Org, OperationLogId = audit.OperationLogId, EffectiveDate = log.Date });
            db.LateCheckInRequests.Add(new() { RequestId = request.RequestId, Request = request, JobId = job.JobId, OrganisationId = actor.Org,
                IndividualId = actor.Person, CreatedByUserId = actor.User, AttendanceLogId = logId, WorkAssignmentId = assignment,
                ScheduleName = fact.Name, ScheduledStart = fact.ScheduledStart, ActualCheckIn = log.Date,
                LateMinutes = (int)Math.Ceiling((log.Date - fact.ScheduledStart).TotalMinutes), Reason = reason.Trim(), CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            await workflow.StartAsync(db, request.RequestId, job.JobId);
            await tx.CommitAsync();
            return ServiceResult.Ok($"{request.ReferenceNumber} submitted for approval. Your recorded check-in time is unchanged.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); if (ex is WorkflowConfigurationException config) await config.NotifyAsync(factory); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The request or attendance record changed. Refresh and try again."); }
    }
    private static Task<WfRequestInstance?> Instance(HrmTeContext db, int requestId) => db.WfRequestInstances
        .Include(i => i.WorkflowDefinition).ThenInclude(d => d.Stages)
        .Include(i => i.CurrentStage).ThenInclude(s => s!.WorkflowStage)
        .Include(i => i.CurrentStage).ThenInclude(s => s!.Approvals).SingleOrDefaultAsync(i => i.RequestId == requestId);
    private async Task<List<WorkflowApproverCandidate>> Candidates(HrmTeContext db, LateCheckInRequest request, int stage)
    {
        var context = await contextResolver.ResolveAsync(db, request.RequestId, request.JobId);
        var candidates = await resolver.ResolveAsync(stage, context);
        var ids = candidates.Select(c => c.UserId).ToList();
        var allowed = await db.Users.Where(u => ids.Contains(u.UserId) && u.BusinessEntityID != request.IndividualId && u.IsActive &&
            u.LocalUserStateId == 1 && u.UserOrganisation!.BusinessEntityID == request.OrganisationId).Select(u => u.UserId).ToListAsync();
        return candidates.Where(c => allowed.Contains(c.UserId)).DistinctBy(c => c.UserId).ToList();
    }
    private async Task<bool> CanApprove(HrmTeContext db, LateCheckInRequest request, WfRequestInstance? instance, int user, int person)
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
    public async Task<List<LateCheckInRow>> LoadAsync(DateTime from, DateTime to)
    {
        if (from > to || (to - from).TotalDays > 366) throw new InvalidOperationException("Choose a date range of up to one year.");
        var actor = await Scope();
        await using var db = await factory.CreateDbContextAsync();
        var rows = await db.LateCheckInRequests.Include(r => r.Request).ThenInclude(r => r.RequestState)
            .Where(r => r.OrganisationId == actor.Org && r.ScheduledStart >= from.Date && r.ScheduledStart < to.Date.AddDays(1) &&
                (r.IndividualId == actor.Person || db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.User && a.RequestStageInstance.WorkflowRequestInstance.RequestId == r.RequestId)))
            .OrderByDescending(r => r.CreatedAtUtc).Take(200).ToListAsync();
        var result = new List<LateCheckInRow>();
        foreach (var r in rows)
        {
            var i = await Instance(db, r.RequestId);
            var name = await db.Individuals.Where(p => p.BusinessEntityId == r.IndividualId).Select(p => p.FirstNameEnglish + " " + p.LastNameEnglish).SingleAsync();
            result.Add(new(r.RequestId, r.Request.RequestStateId, r.Request.ReferenceNumber ?? $"#{r.RequestId}", name, r.ScheduleName,
                r.ScheduledStart, r.ActualCheckIn, r.LateMinutes, r.Reason,
                (r.Request.RequestState.IsProcessingState && !r.Request.RequestState.IsFinalState ? "Processing · " : "") + r.Request.RequestState.StateName,
                i?.CurrentStage?.WorkflowStage.StageName ?? "", await CanApprove(db, r, i, actor.User, actor.Person),
                r.IndividualId == actor.Person && !r.Request.RequestState.IsFinalState && i?.WorkflowStatus == "InProgress",
                i == null ? [] : await db.WfRequestActions.Where(a => a.WorkflowRequestInstanceId == i.WorkflowRequestInstanceId).OrderBy(a => a.PerformedAt)
                    .Select(a => a.ActionCode + ": " + (a.Comments ?? "")).ToListAsync()));
        }
        return result;
    }
    private static void Notify(HrmTeContext db, LateCheckInRequest request, int user, long stage) => db.RequestNotifications.Add(new()
    { RequestId = request.RequestId, UserId = user, NotificationKind = "ActionRequired", RequestStageInstanceId = stage,
        FromStateId = request.Request.RequestStateId, ToStateId = request.Request.RequestStateId, ReferenceNumber = request.Request.ReferenceNumber ?? "Late check-in request", CreatedAtUtc = DateTime.UtcNow });
    public async Task<ServiceResult> ActAsync(int id, int expectedState, string action, string? comment)
    {
        if (action is not ("Approve" or "Reject" or "Cancel")) return ServiceResult.Fail("Unsupported action.");
        if (comment?.Length > 2000 || action != "Approve" && string.IsNullOrWhiteSpace(comment)) return ServiceResult.Fail("Enter an action comment of up to 2,000 characters.");
        var actor = await Scope();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var r = await db.LateCheckInRequests.Include(r => r.Request).ThenInclude(r => r.RequestState)
                .SingleOrDefaultAsync(r => r.RequestId == id && r.OrganisationId == actor.Org) ?? throw new UnauthorizedAccessException("Request not available in this organisation.");
            var instance = await Instance(db, id) ?? throw new InvalidOperationException("The request is no longer available for action.");
            var stage = instance.CurrentStage ?? throw new InvalidOperationException("Refresh the request before acting.");
            if (r.Request.RequestStateId != expectedState || r.Request.RequestState.IsFinalState || instance.WorkflowStatus != "InProgress" || instance.CompletedAt.HasValue)
                throw new InvalidOperationException("The request changed. Refresh before acting.");
            if (action == "Cancel" ? r.IndividualId != actor.Person : !await CanApprove(db, r, instance, actor.User, actor.Person))
                throw new UnauthorizedAccessException("You are not assigned to perform this action. You cannot approve your own request.");
            var from = r.Request.RequestStateId; var to = action == "Reject" ? 6 : action == "Cancel" ? 5 : from;
            if (action == "Approve")
            {
                var currentFacts = await AttendanceScheduleFacts.LoadAsync(db, r.IndividualId, r.ScheduledStart.Date.AddDays(-1), r.ScheduledStart.Date, r.OrganisationId);
                if (!currentFacts.Any(f => f.WorkAssignmentId == r.WorkAssignmentId && f.CheckInLogId == r.AttendanceLogId &&
                    f.CheckIn == r.ActualCheckIn && f.ScheduledStart == r.ScheduledStart && f.RequiresAttendance))
                    throw new InvalidOperationException("The attendance or schedule changed after submission. Reject or cancel this request and submit a new explanation.");
                var own = stage.Approvals.Single(a => a.ApproverUserId == actor.User);
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
                    if (next == null) { instance.WorkflowStatus = "Approved"; instance.ExecutionStatus = "Succeeded"; instance.CompletedAt = DateTime.UtcNow; }
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
                foreach (var a in stage.Approvals.Where(a => a.Decision == "Pending")) { a.Decision = action == "Reject" && a.ApproverUserId == actor.User ? "Rejected" : "Skipped"; a.DecisionAt = DateTime.UtcNow; a.Comments = comment; }
            }
            if (await db.RequestStates.AnyAsync(s => s.RequestStateId == to && s.CommentRequired) && string.IsNullOrWhiteSpace(comment)) throw new InvalidOperationException("Enter a comment for this state change.");
            r.Request.RequestStateId = to; r.Request.LastStateChangedByUserId = actor.User; r.Request.LastStateChangedDate = DateTime.UtcNow; r.Request.StateChangeRemarks = comment;
            db.WfRequestActions.Add(new() { WorkflowRequestInstance = instance, RequestStageInstance = stage, PerformedByUserId = actor.User,
                ActionCode = action, FromRequestStateId = from, ToRequestStateId = to, Comments = comment, PerformedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok(to == 4 ? "Late check-in explanation approved. The recorded clock time and late minutes remain unchanged." : "Request action saved.");
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            if (ex is WorkflowConfigurationException configuration) await configuration.NotifyAsync(factory);
            if (ex is RequestTransitionConfigurationException transition)
            {
                var subject = await db.LateCheckInRequests.AsNoTracking().SingleAsync(r => r.RequestId == id && r.OrganisationId == actor.Org);
                var context = await contextResolver.ResolveAsync(db, id, subject.JobId);
                await new WorkflowConfigurationException(context, transition.Detail) { ConfigurationUrl = "/admin/settings/states" }.NotifyAsync(factory);
            }
            return ServiceResult.Fail(ex.Message);
        }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The request changed. Refresh and try again."); }
    }
}
