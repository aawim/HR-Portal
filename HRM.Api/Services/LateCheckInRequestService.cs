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



public sealed partial class LateCheckInRequestService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IAttendanceService attendance, IRequestService requests, IOperationLogService logs, IWorkflowEngine workflow,
    IWorkflowApproverResolver resolver, IWorkflowRequestContextResolver contextResolver)
:global::HRM.Services.Contracts.LateCheckInRequestServiceContracts,global::HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations{
    
    
    private static DateTime Today => DateTime.UtcNow.AddHours(5).Date;
    private async Task<(int User, int Person, int Org, int Job)> Scope()
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select your current organisation.");
        return (actor.UserId, actor.IndividualId, org, actor.ActiveJob?.JobId ?? 0);
    }
    
    
    public async Task<ServiceResult> SubmitForDateAsync(DateTime date, TimeOnly time, string reason, string kind)
    {
        try
        {
            var candidates = await GetCandidatesAsync(date.Date, date.Date, kind);
            var schedule = AttendanceTimeChangeRules.ResolveSchedule(candidates, time, kind);
            return await SubmitAsync(schedule.WorkAssignmentId, AttendanceTimeChangeRules.RequestedDateTime(schedule, time), reason, kind);
        }
        catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
    }
    public async Task<List<StaffScheduleAttendanceDto>> GetCandidatesAsync(DateTime from, DateTime to, string kind = "LateCheckIn")
    {
        var actor = await Scope();
        var facts = await attendance.GetStaffScheduleAttendanceAsync(actor.Person, from, to);
        await using var db = await factory.CreateDbContextAsync();
        var used = await db.LateCheckInRequests.Where(r => r.OrganisationId == actor.Org && r.IndividualId == actor.Person &&
            r.Kind == kind && r.Request.RequestStateId != 5 && r.Request.RequestStateId != 6).Select(r => r.WorkAssignmentId).ToListAsync();
        return facts.Where(f => f.RequiresAttendance && f.ScheduledEnd > f.ScheduledStart && !used.Contains(f.WorkAssignmentId)).ToList();
    }
    public async Task<ServiceResult> SubmitAsync(long assignment, DateTime requestedTime, string reason, string kind = "LateCheckIn")
    {
        if (kind is not ("LateCheckIn" or "EarlyCheckOut")) return ServiceResult.Fail("Select a valid request type.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) return ServiceResult.Fail("Enter a reason of up to 1,000 characters.");
        var actor = await Scope();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var workDate = await db.WorkAssignments.Where(a => a.WorkAssignmentId == assignment && a.IsValid && a.CancelledDate == null &&
                a.WorkPlan.IsValid && a.WorkPlan.OrganisationBusinessEntityId == actor.Org).Select(a => (DateTime?)a.WorkPlan.WorkDate).SingleOrDefaultAsync()
                ?? throw new InvalidOperationException("This schedule is no longer available.");
            var facts = await AttendanceScheduleFacts.LoadAsync(db, actor.Person, workDate.Date, workDate.Date, actor.Org);
            var fact = facts.SingleOrDefault(f => f.WorkAssignmentId == assignment && f.RequiresAttendance)
                ?? throw new InvalidOperationException("Select one of your required schedules in the current organisation.");
            var proposed = AttendanceTimeChangeRules.Calculate(fact.ScheduledStart, fact.ScheduledEnd, requestedTime, kind);
            var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(j => j.JobId == actor.Job && j.IndividualID == actor.Person && j.OrganisationID == actor.Org &&
                j.JobStateId == 4 && j.JoinedDate <= fact.Date && (j.TerminatedDate == null || j.TerminatedDate > fact.Date))
                ?? throw new InvalidOperationException("Active approved employment is required for this request.");
            if (await db.Leaves.AnyAsync(l => l.Job.IndividualID == actor.Person && l.Job.OrganisationID == actor.Org && l.LeaveStateId == 4 &&
                l.FromDate < fact.Date.AddDays(1) && l.ToDate >= fact.Date))
                throw new InvalidOperationException("This date is covered by approved leave. Please contact HR to reconcile it first.");
            if (await db.LateCheckInRequests.AnyAsync(r => r.WorkAssignmentId == assignment && r.Kind == kind && r.Request.RequestStateId != 5 && r.Request.RequestStateId != 6))
                throw new InvalidOperationException("A pending or approved request of this type already exists for this schedule.");
            if (await db.LateCheckInRequests.AnyAsync(r => r.WorkAssignmentId == assignment && r.Request.RequestState.IsProcessingState && !r.Request.RequestState.IsFinalState))
                throw new InvalidOperationException("This schedule already has a pending time-change request. Complete it before submitting another.");
            var typeName = kind == "EarlyCheckOut" ? EarlyTypeName : TypeName;
            var type = await db.RequestTypes.SingleAsync(t => t.TypeName == typeName);
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.ATTENDANCE_LOG_CREATE, typeName + " submitted; schedule changes only after final approval.");
            var request = await requests.CreateAsync(db, new() { RequestTypeId = type.RequestTypeId, ApplicantBusinessEntityId = actor.Person,
                OrganisationBusinessEntityId = actor.Org, OperationLogId = audit.OperationLogId, EffectiveDate = fact.Date });
            db.LateCheckInRequests.Add(new() { RequestId = request.RequestId, Request = request, JobId = job.JobId, OrganisationId = actor.Org,
                IndividualId = actor.Person, CreatedByUserId = actor.User, AttendanceLogId = fact.CheckInLogId, WorkAssignmentId = assignment,
                Kind = kind, ScheduleName = fact.Name, ScheduledStart = fact.ScheduledStart, ScheduledEnd = fact.ScheduledEnd, ActualCheckIn = fact.CheckIn,
                RequestedStart = proposed.Start, RequestedEnd = proposed.End,
                LateMinutes = (int)Math.Ceiling(Math.Abs((requestedTime - (kind == "EarlyCheckOut" ? fact.ScheduledEnd : fact.ScheduledStart)).TotalMinutes)),
                Reason = reason.Trim(), CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            await workflow.StartAsync(db, request.RequestId, job.JobId);
            await tx.CommitAsync();
            return ServiceResult.Ok(request.ReferenceNumber + " submitted for approval. Check-out will be compulsory after final approval.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); if (ex is WorkflowConfigurationException config) await config.NotifyAsync(factory); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The request or schedule changed. Refresh and try again."); }
    }
    private WorkflowRequestProcessing Processing => new(resolver, contextResolver);
    private static WorkflowRequestSubject Subject(LateCheckInRequest r) => new(r.Request, r.JobId, r.IndividualId, r.OrganisationId);
    private static Task<WfRequestInstance?> Instance(HrmTeContext db, int id) => WorkflowRequestProcessing.Instance(db, id);
    private Task<bool> CanApprove(HrmTeContext db, LateCheckInRequest r, WfRequestInstance? i, int user, int person) =>
        Processing.CanApprove(db, Subject(r), i, user, person);
    public async Task<List<LateCheckInRow>> LoadAsync(DateTime from, DateTime to, string? kind = null, bool pendingOnly = false)
    {
        if (from > to || (to - from).TotalDays > 366) throw new InvalidOperationException("Choose a date range of up to one year.");
        var actor = await Scope();
        await using var db = await factory.CreateDbContextAsync();
        var rows = await db.LateCheckInRequests.Include(r => r.Request).ThenInclude(r => r.RequestState)
            .Where(r => r.OrganisationId == actor.Org && (kind == null || r.Kind == kind || kind == "LateCheckIn" && r.Kind == "Explanation") &&
                (pendingOnly ? r.Request.RequestState.IsProcessingState && !r.Request.RequestState.IsFinalState : r.ScheduledStart >= from.Date && r.ScheduledStart < to.Date.AddDays(1)) &&
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
                    .Select(a => a.ActionCode + ": " + (a.Comments ?? "")).ToListAsync(), r.Kind, r.RequestedStart, r.RequestedEnd));
        }
        return result;
    }
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
            var to = await Processing.ExecuteAsync(db, Subject(r), expectedState, action, comment, actor.User, actor.Person,
                async () =>
                {
                    var currentFacts = await AttendanceScheduleFacts.LoadAsync(db, r.IndividualId, r.ScheduledStart.Date.AddDays(-1), r.ScheduledStart.Date, r.OrganisationId);
                    if (!currentFacts.Any(f => f.WorkAssignmentId == r.WorkAssignmentId && f.ScheduledStart == r.ScheduledStart && f.RequiresAttendance &&
                        (r.Kind == "Explanation" ? f.CheckInLogId == r.AttendanceLogId && f.CheckIn == r.ActualCheckIn : f.ScheduledEnd == r.ScheduledEnd)))
                        throw new InvalidOperationException("The attendance or schedule changed after submission. Reject or cancel this request and submit a new explanation.");
                },
                async () => { if (r.Kind != "Explanation") await ApplyScheduleAsync(db, r); });
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok(to == 4 ? r.Kind == "Explanation" ? "Late check-in explanation approved." : "Time-change request approved. The schedule has been updated and check-out is compulsory." : "Request action saved.");
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            if (ex is WorkflowConfigurationException configuration) await configuration.NotifyAsync(factory);
            if (ex is RequestTransitionConfigurationException transition)
            {
                var subject = await db.LateCheckInRequests.AsNoTracking().SingleAsync(r => r.RequestId == id && r.OrganisationId == actor.Org);
                var context = await contextResolver.ResolveAsync(db, id, subject.JobId);
                await new WorkflowConfigurationException(context, transition.Detail) { ConfigurationUrl = "/admin/settings/config/state-transitions" }.NotifyAsync(factory);
            }
            return ServiceResult.Fail(ex.Message);
        }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The request changed. Refresh and try again."); }
    }
}
