using System.Data;
using HRM.DTOs;
using HRM.Helpers;
using HRM.Models;
using HRM.Components.Shared;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed record OvertimeClockOption(int Id, DateTime Time);
public sealed record OvertimeTaskOption(int Id, int RequestId, string Reference, string Task, DateTime From, DateTime To);
public sealed record OvertimeWorkRow(int Id, int StateId, string Reference, string Staff, string Kind,
    DateTime From, DateTime To, string Reason, List<string> Tasks, string Status, bool CanApprove, bool CanCancel,
    DateTime? Start, DateTime? End);

public sealed partial class OvertimeRequestService
{
    public const string PreapprovalTypeName = "Overtime Preapproval Request";
    public const string ClockTypeName = "Overtime Clock Event Request";

    private static async Task<Job> EligibleJobAsync(HrmTeContext db, int org, int person, DateTime from, DateTime to)
    {
        var job = await db.Jobs.Where(j => j.IndividualID == person && j.OrganisationID == org && j.JobStateId == 4 &&
            j.JoinedDate <= from && (!j.TerminatedDate.HasValue || j.TerminatedDate > to))
            .OrderByDescending(j => j.JoinedDate).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Approved employment is required for these dates.");
        if (!await db.JobTypeOvertimeRules.AnyAsync(p => p.OrganisationId == org && p.JobTypeId == job.JobTypeId && p.IsAllowed))
            throw new InvalidOperationException("Overtime is not enabled for your Job Type. Contact HR.");
        return job;
    }
    // All timestamps are local schedule time, as stored by the existing attendance system.
    private static async Task ValidateOutsideOfficialAsync(HrmTeContext db, int org, int person, DateTime start, DateTime end)
    {
        if (end <= start || end > DateTime.UtcNow.AddHours(5) || end - start > TimeSpan.FromHours(24) || start.Date < Today.AddMonths(-3))
            throw new InvalidOperationException("Select a completed interval of up to 24 hours within the last three months.");
        var facts = await AttendanceScheduleFacts.LoadAsync(db, person, start.Date.AddDays(-1), end.Date, org);
        var official = facts.Where(f => f.Purpose == "Official").ToList();
        for (var day = start.Date; day < end; day = day.AddDays(1))
        {
            if (official.Any(f => f.ScheduledStart < day.AddDays(1) && f.ScheduledEnd > day)) continue;
            var job = await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == org && j.IndividualID == person && j.JobStateId == 4 &&
                j.JoinedDate <= day && (!j.TerminatedDate.HasValue || j.TerminatedDate > day)).OrderByDescending(j => j.JoinedDate).FirstOrDefaultAsync();
            var schedules = job == null ? [] : EffectiveStaffSchedules.On(await EffectiveStaffSchedules.LoadAsync(db, job, day, day), day)
                .Where(c => c.Assignment.SchedulePurpose == "Official").ToList();
            if (schedules.Count == 0 || schedules.Any(c => StaffScheduleCalendar.TemplateOn(c.Assignment, day) > 0))
                throw new InvalidOperationException("An official work plan is needed to verify overtime for this date. Contact HR.");
            // A configured official schedule with an off day has no official hours to exclude.
        }
        if (official.Any(f => start < f.ScheduledEnd && end > f.ScheduledStart))
            throw new InvalidOperationException("The selected interval includes official working hours. Select clock events entirely outside official time.");
    }
    private static async Task ValidateEvidenceAsync(HrmTeContext db, OvertimeRequest r)
    {
        if (!r.StartLogId.HasValue || !r.EndLogId.HasValue || r.StartLogId == r.EndLogId)
            throw new InvalidOperationException("Select two different saved clock events.");
        var ids = new[] { r.StartLogId.Value, r.EndLogId.Value };
        var events = await db.AttendanceLogs.AsNoTracking().Where(l => ids.Contains(l.AttendanceLogId) &&
            l.OrganisationId == r.OrganisationId && l.IndividualId == r.IndividualId && l.AttendanceLogStateId == 1).ToListAsync();
        if (events.Count != 2) throw new InvalidOperationException("A clock event is unavailable or belongs to another staff member.");
        var start = events.Single(l => l.AttendanceLogId == r.StartLogId).Date;
        var end = events.Single(l => l.AttendanceLogId == r.EndLogId).Date;
        if (r.StartedAt != start || r.EndedAt != end || r.WorkDate != start.Date || r.Minutes != (int)(end - start).TotalMinutes || r.Minutes < 1)
            throw new InvalidOperationException("Clock evidence changed. Refresh and select the events again.");
        await ValidateOutsideOfficialAsync(db, r.OrganisationId, r.IndividualId, start, end);
        if (await db.OvertimeRequests.AnyAsync(x => x.RequestId != r.RequestId && x.OrganisationId == r.OrganisationId &&
            x.IndividualId == r.IndividualId && x.WorkDate == r.WorkDate && x.ClaimSource == "Legacy" &&
            (!x.Request.RequestState.IsFinalState || x.Request.RequestStateId == 4)))
            throw new InvalidOperationException("HR must review the existing legacy claim for this date before further overtime is claimed.");
        if (await db.OvertimeRequests.AnyAsync(x => x.RequestId != r.RequestId && x.OrganisationId == r.OrganisationId &&
            x.IndividualId == r.IndividualId && (!x.Request.RequestState.IsFinalState || x.Request.RequestStateId == 4) &&
            (x.StartLogId == r.StartLogId || x.StartLogId == r.EndLogId || x.EndLogId == r.StartLogId || x.EndLogId == r.EndLogId ||
             x.StartedAt <= end && x.EndedAt >= start)))
            throw new InvalidOperationException("These clock events or times are already used by a pending or approved OT claim. Cancel that request before using them again.");
        if (r.ClaimSource == "Planned")
        {
            var task = await db.OvertimeWorkTasks.Include(t => t.WorkRequest).ThenInclude(w => w.Request)
                .SingleOrDefaultAsync(t => t.Id == r.PreapprovalTaskId);
            if (task == null || task.WorkRequest.Kind != "Preapproval" || task.WorkRequest.Request.RequestStateId != 4 ||
                task.WorkRequest.OrganisationId != r.OrganisationId || task.WorkRequest.IndividualId != r.IndividualId ||
                task.WorkRequest.JobId != r.JobId || start.Date < task.WorkRequest.FromDate || end > task.WorkRequest.ToDate.AddDays(1))
                throw new InvalidOperationException("Select an approved task for this staff member and work period.");
        }
        else if (r.ClaimSource != "Unplanned" || r.PreapprovalTaskId.HasValue)
            throw new InvalidOperationException("Invalid OT claim source.");
    }
    private static async Task ValidateCapsAsync(HrmTeContext db, OvertimeRequest r, JobTypeOvertimeRule policy)
    {
        var start = r.StartedAt!.Value; var end = r.EndedAt!.Value;
        var from = r.WeekStart.AddDays(-1); var to = r.WeekStart.AddDays(14);
        var others = await db.OvertimeRequests.AsNoTracking().Where(x => x.RequestId != r.RequestId && x.OrganisationId == r.OrganisationId &&
            x.IndividualId == r.IndividualId && x.WorkDate >= from && x.WorkDate < to &&
            (!x.Request.RequestState.IsFinalState || x.Request.RequestStateId == 4)).ToListAsync();
        static decimal Minutes(OvertimeRequest x, DateTime a, DateTime b)
        {
            if (!x.StartedAt.HasValue || !x.EndedAt.HasValue) return x.WorkDate >= a && x.WorkDate < b ? x.Minutes : 0;
            var lo = x.StartedAt.Value > a ? x.StartedAt.Value : a;
            var hi = x.EndedAt.Value < b ? x.EndedAt.Value : b;
            return hi > lo ? (decimal)(hi - lo).TotalMinutes : 0;
        }
        foreach (var week in new[] { r.WeekStart, r.WeekStart.AddDays(7) })
            if (Minutes(r, week, week.AddDays(7)) > 0 && others.Sum(x => Minutes(x, week, week.AddDays(7))) + Minutes(r, week, week.AddDays(7)) > policy.MaximumWeeklyHours * 60)
                throw new InvalidOperationException("This claim exceeds the weekly OT allowance, including pending claims.");
        for (var day = start.Date; day < end; day = day.AddDays(1))
            if (policy.MaximumDailyHours.HasValue && others.Sum(x => Minutes(x, day, day.AddDays(1))) + Minutes(r, day, day.AddDays(1)) > policy.MaximumDailyHours.Value * 60)
                throw new InvalidOperationException("This claim exceeds the daily OT allowance, including pending claims.");
    }
    public async Task<List<OvertimeClockOption>> LoadClockEventsAsync(DateTime date)
    {
        var actor = await ScopeAsync(); date = date.Date;
        await using var db = await factory.CreateDbContextAsync();
        await EligibleJobAsync(db, actor.Org, actor.Person, date, date);
        var end = date.AddDays(2);
        var events = await db.AttendanceLogs.AsNoTracking().Where(l => l.OrganisationId == actor.Org && l.IndividualId == actor.Person &&
            l.AttendanceLogStateId == 1 && l.Date >= date && l.Date < end && l.Date <= DateTime.UtcNow.AddHours(5))
            .OrderBy(l => l.Date).Select(l => new OvertimeClockOption(l.AttendanceLogId, l.Date)).ToListAsync();
        var official = (await AttendanceScheduleFacts.LoadAsync(db, actor.Person, date.AddDays(-1), date.AddDays(1), actor.Org))
            .Where(f => f.Purpose == "Official").ToList();
        var reserved = await db.OvertimeRequests.AsNoTracking().Where(x => x.OrganisationId == actor.Org && x.IndividualId == actor.Person &&
            (!x.Request.RequestState.IsFinalState || x.Request.RequestStateId == 4) &&
            (x.WorkDate >= date.AddDays(-1) && x.WorkDate < end || x.StartedAt < end && x.EndedAt >= date))
            .Select(x => new { x.StartLogId, x.EndLogId, x.StartedAt, x.EndedAt, x.WorkDate, x.ClaimSource }).ToListAsync();
        return events.Where(e => !official.Any(f => e.Time > f.ScheduledStart && e.Time < f.ScheduledEnd) &&
            !reserved.Any(r => r.StartLogId == e.Id || r.EndLogId == e.Id ||
                r.StartedAt.HasValue && r.EndedAt.HasValue && e.Time >= r.StartedAt && e.Time <= r.EndedAt ||
                r.ClaimSource == "Legacy" && e.Time.Date == r.WorkDate)).ToList();
    }
    public async Task<List<OvertimeTaskOption>> LoadApprovedTasksAsync()
    {
        var actor = await ScopeAsync(); await using var db = await factory.CreateDbContextAsync();
        return await db.OvertimeWorkTasks.AsNoTracking().Where(t => t.WorkRequest.OrganisationId == actor.Org &&
            t.WorkRequest.IndividualId == actor.Person && t.WorkRequest.Kind == "Preapproval" && t.WorkRequest.Request.RequestStateId == 4)
            .OrderByDescending(t => t.WorkRequest.FromDate).Select(t => new OvertimeTaskOption(t.Id, t.WorkRequestId,
                t.WorkRequest.Request.ReferenceNumber ?? "OT", t.Description, t.WorkRequest.FromDate, t.WorkRequest.ToDate)).ToListAsync();
    }
    public async Task<ServiceResult> SubmitClaimAsync(int startLogId, int endLogId, string reason, int? taskId = null)
    {
        var actor = await ScopeAsync();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) return ServiceResult.Fail("Enter the work performed, up to 1,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var events = await db.AttendanceLogs.Where(l => (l.AttendanceLogId == startLogId || l.AttendanceLogId == endLogId) &&
                l.OrganisationId == actor.Org && l.IndividualId == actor.Person && l.AttendanceLogStateId == 1).ToListAsync();
            if (events.Count != 2) throw new InvalidOperationException("Select saved start and end clock events belonging to you.");
            var start = events.Single(l => l.AttendanceLogId == startLogId).Date; var end = events.Single(l => l.AttendanceLogId == endLogId).Date;
            var job = await EligibleJobAsync(db, actor.Org, actor.Person, start.Date, end.Date);
            var weekDay = await db.JobTypeWorkingHourRules.Where(p => p.OrganisationId == actor.Org && p.JobTypeId == job.JobTypeId &&
                p.EffectiveFrom <= start.Date && (!p.EffectiveTo.HasValue || p.EffectiveTo >= start.Date)).Select(p => (int?)p.WeekStartsOn).SingleOrDefaultAsync() ?? 0;
            var row = new OvertimeRequest { OrganisationId = actor.Org, IndividualId = actor.Person, JobId = job.JobId,
                WorkDate = start.Date, WeekStart = start.Date.AddDays(-((7 + (int)start.DayOfWeek - weekDay) % 7)),
                StartLogId = startLogId, EndLogId = endLogId, StartedAt = start, EndedAt = end,
                Minutes = (int)(end - start).TotalMinutes, PreapprovalTaskId = taskId, ClaimSource = taskId.HasValue ? "Planned" : "Unplanned",
                Reason = reason.Trim(), CreatedAtUtc = DateTime.UtcNow };
            await ValidateAsync(db, row);
            var type = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == TypeName) ?? throw new InvalidOperationException("OT claims are unavailable. Contact HR.");
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"OT claim: org {actor.Org}, person {actor.Person}, logs {startLogId}/{endLogId}, task {taskId}.");
            row.Request = await requests.CreateAsync(db, new CreateRequestDto { RequestTypeId = type.RequestTypeId, ApplicantBusinessEntityId = actor.Person,
                OrganisationBusinessEntityId = actor.Org, EffectiveDate = start.Date, OperationLogId = audit.OperationLogId });
            row.RequestId = row.Request.RequestId; db.OvertimeRequests.Add(row); await db.SaveChangesAsync();
            await workflow.StartAsync(db, row.RequestId, job.JobId); await tx.CommitAsync();
            return ServiceResult.Ok("Overtime claim submitted for approval.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); if (ex is WorkflowConfigurationException config) await config.NotifyAsync(factory); return ServiceResult.Fail(ex.Message); }
    }
    public async Task<ServiceResult> SubmitWorkAsync(string kind, DateTime from, DateTime to, string reason, List<string> tasks,
        DateTime? proposedStart = null, DateTime? proposedEnd = null)
    {
        var actor = await ScopeAsync(); from = from.Date; to = to.Date;
        if (kind is not ("Preapproval" or "ClockEvents") || to < from || (to - from).Days > 366 || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
            return ServiceResult.Fail("Enter a valid date range and a reason of up to 1,000 characters.");
        var descriptions = tasks.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (kind == "Preapproval" && (from < Today || descriptions.Count is < 1 or > 30 || descriptions.Any(t => t.Length > 500)))
            return ServiceResult.Fail("Select today or future dates and enter 1–30 tasks, up to 500 characters each.");
        if (kind == "ClockEvents" && (!proposedStart.HasValue || !proposedEnd.HasValue || proposedStart.Value.Date != from || proposedEnd.Value.Date != to))
            return ServiceResult.Fail("Enter the missing OT start and end punches with their dates.");
        await using var db = await factory.CreateDbContextAsync(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var job = await EligibleJobAsync(db, actor.Org, actor.Person, from, to);
            if (kind == "ClockEvents") await ValidateOutsideOfficialAsync(db, actor.Org, actor.Person, proposedStart!.Value, proposedEnd!.Value);
            if (kind == "ClockEvents" && await db.OvertimeWorkRequests.AnyAsync(w => w.Kind == kind && w.OrganisationId == actor.Org && w.IndividualId == actor.Person &&
                (!w.Request.RequestState.IsFinalState || w.Request.RequestStateId == 4) && w.ProposedStart < proposedEnd && w.ProposedEnd > proposedStart))
                throw new InvalidOperationException("An OT clock request already covers this interval.");
            var name = kind == "Preapproval" ? PreapprovalTypeName : ClockTypeName;
            var type = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == name) ?? throw new InvalidOperationException("This OT request is unavailable. Contact HR.");
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"OT {kind}: org {actor.Org}, person {actor.Person}, {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");
            var request = await requests.CreateAsync(db, new CreateRequestDto { RequestTypeId = type.RequestTypeId, ApplicantBusinessEntityId = actor.Person,
                OrganisationBusinessEntityId = actor.Org, EffectiveDate = from, OperationLogId = audit.OperationLogId });
            db.OvertimeWorkRequests.Add(new() { Request = request, RequestId = request.RequestId, OrganisationId = actor.Org,
                IndividualId = actor.Person, JobId = job.JobId, Kind = kind, FromDate = from, ToDate = to, Reason = reason.Trim(),
                ProposedStart = kind == "ClockEvents" ? proposedStart : null, ProposedEnd = kind == "ClockEvents" ? proposedEnd : null,
                CreatedAtUtc = DateTime.UtcNow, Tasks = kind == "Preapproval" ? descriptions.Select(t => new OvertimeWorkTask { Description = t }).ToList() : [] });
            await db.SaveChangesAsync(); await workflow.StartAsync(db, request.RequestId, job.JobId); await tx.CommitAsync();
            return ServiceResult.Ok("Overtime request submitted for approval.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); if (ex is WorkflowConfigurationException config) await config.NotifyAsync(factory); return ServiceResult.Fail(ex.Message); }
    }
    public async Task<List<OvertimeWorkRow>> LoadWorkAsync(bool pendingOnly = false)
    {
        var actor = await ScopeAsync(); await using var db = await factory.CreateDbContextAsync();
        var rows = await db.OvertimeWorkRequests.AsNoTracking().Include(w => w.Tasks).Include(w => w.Request).ThenInclude(r => r.RequestState)
            .Where(w => w.OrganisationId == actor.Org && (!pendingOnly || !w.Request.RequestState.IsFinalState) &&
                (actor.Manage || w.IndividualId == actor.Person || db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.User && a.RequestStageInstance.WorkflowRequestInstance.RequestId == w.RequestId)))
            .OrderByDescending(w => w.CreatedAtUtc).Take(100).ToListAsync();
        var result = new List<OvertimeWorkRow>();
        foreach (var w in rows)
        {
            var instance = await WorkflowRequestProcessing.Instance(db, w.RequestId);
            var subject = new WorkflowRequestSubject(w.Request, w.JobId, w.IndividualId, w.OrganisationId);
            var staff = await db.Individuals.Where(i => i.BusinessEntityId == w.IndividualId).Select(i => i.FirstNameEnglish + " " + i.LastNameEnglish).SingleAsync();
            result.Add(new(w.RequestId, w.Request.RequestStateId, w.Request.ReferenceNumber ?? $"#{w.RequestId}", staff, w.Kind,
                w.FromDate, w.ToDate, w.Reason, w.Tasks.OrderBy(t => t.Id).Select(t => t.Description).ToList(),
                (w.Request.RequestState.IsFinalState ? "" : "Processing · ") + w.Request.RequestState.StateName,
                await Processing.CanApprove(db, subject, instance, actor.User, actor.Person), w.IndividualId == actor.Person &&
                !w.Request.RequestState.IsFinalState && instance?.WorkflowStatus == "InProgress", w.ProposedStart, w.ProposedEnd));
        }
        return result;
    }
    public async Task<ServiceResult> ActWorkAsync(int id, int expectedState, string action, string? comment)
    {
        var actor = await ScopeAsync(); await using var db = await factory.CreateDbContextAsync(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var w = await db.OvertimeWorkRequests.Include(x => x.Request).ThenInclude(x => x.RequestState)
                .SingleOrDefaultAsync(x => x.RequestId == id && x.OrganisationId == actor.Org) ?? throw new UnauthorizedAccessException("Request unavailable in this organisation.");
            async Task Validate()
            {
                var eligible = await EligibleJobAsync(db, w.OrganisationId, w.IndividualId, w.FromDate, w.ToDate);
                if (eligible.JobId != w.JobId) throw new InvalidOperationException("Employment changed. Submit a new request for the current job.");
                if (w.Kind == "ClockEvents") await ValidateOutsideOfficialAsync(db, w.OrganisationId, w.IndividualId, w.ProposedStart!.Value, w.ProposedEnd!.Value);
            }
            await Processing.ExecuteAsync(db, new(w.Request, w.JobId, w.IndividualId, w.OrganisationId), expectedState, action, comment,
                actor.User, actor.Person, Validate, async () =>
                {
                    if (w.Kind != "ClockEvents") return;
                    var job = await db.Jobs.SingleAsync(j => j.JobId == w.JobId);
                    var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Approved OT clock request {id}; saved evidence only, no official schedule change.");
                    async Task<AttendanceLog> SavePunch(DateTime time, int mode)
                    {
                        var existing = await db.AttendanceLogs.Where(l => l.OrganisationId == w.OrganisationId && l.IndividualId == w.IndividualId &&
                            l.AttendanceLogStateId == 1 && l.Date == time).OrderBy(l => l.AttendanceLogId).FirstOrDefaultAsync();
                        if (existing != null) return existing;
                        var log = new AttendanceLog { IndividualId = w.IndividualId, OrganisationId = w.OrganisationId, OrganisationStructureId = job.OrganisationStructureId,
                            Date = time, Year = time.Year, Month = time.Month, Day = time.Day, Hour = time.Hour, Minute = time.Minute, Second = time.Second,
                            InOutModeId = mode, ActualInOutMode = mode, AttendanceLogStateId = 1, AttendanceLogModeId = 2,
                            Uidstamp = $"OT-{id}-{mode}", OperationLogId = audit.OperationLogId };
                        db.AttendanceLogs.Add(log); await db.SaveChangesAsync(); return log;
                    }
                    w.StartLogId = (await SavePunch(w.ProposedStart!.Value, 1)).AttendanceLogId;
                    w.EndLogId = (await SavePunch(w.ProposedEnd!.Value, 2)).AttendanceLogId;
                });
            await tx.CommitAsync(); return ServiceResult.Ok("Overtime request action saved.");
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            if (ex is WorkflowConfigurationException config) await config.NotifyAsync(factory);
            if (ex is RequestTransitionConfigurationException transition)
            {
                var w = await db.OvertimeWorkRequests.AsNoTracking().SingleAsync(x => x.RequestId == id && x.OrganisationId == actor.Org);
                await new WorkflowConfigurationException(await contexts.ResolveAsync(db, id, w.JobId), transition.Detail)
                    { ConfigurationUrl = "/admin/settings/config/state-transitions" }.NotifyAsync(factory);
            }
            return ServiceResult.Fail(ex is RequestTransitionConfigurationException ? "This action could not be completed. HR has been notified." : ex.Message);
        }
    }
}
