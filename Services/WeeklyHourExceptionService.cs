using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record WeeklyHourExceptionRow(int Id, int StateId, string Reference, string Staff, DateTime WeekStart,
    int Minutes, string Reason, string Status, bool CanApprove, bool CanCancel);

public sealed class WeeklyHourExceptionService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IRequestService requests, IOperationLogService logs, IWorkflowEngine workflow,
    IWorkflowApproverResolver resolver, IWorkflowRequestContextResolver contextResolver)
{
    public const string TypeName = "Weekly Hours Exception Request";
    public const string PageUrl = "/online/attendance/attendance-request/weekly-hours-exception";
    private WorkflowRequestProcessing Processing => new(resolver, contextResolver);
    private async Task<(int User, int Person, int Org, int Job)> ScopeAsync()
    {
        var actor = await access.RequireContextAsync(); var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select your current organisation.");
        return (actor.UserId, actor.IndividualId, org, actor.ActiveJob?.JobId ?? 0);
    }
    private static WorkflowRequestSubject Subject(WeeklyHourExceptionRequest r) => new(r.Request, r.JobId, r.IndividualId, r.OrganisationId);
    public async Task<ServiceResult> SubmitAsync(DateTime day, int minutes, string reason)
    {
        if (minutes is < 1 or > 10080 || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return ServiceResult.Fail("Enter the hours to excuse and a reason of up to 1,000 characters.");
        var actor = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var summary = await WeeklyWorkingHours.LoadAsync(db, actor.Person, actor.Org, day);
            if (!summary.HasWorkingHoursRule || summary.RequiredWeekHours <= 0 || Math.Round(summary.RequiredWeekHours * 60) < minutes)
                throw new InvalidOperationException("The requested exception exceeds this week's remaining requirement, or weekly hours are not configured. Please contact HR.");
            var today = DateTime.UtcNow.AddHours(5).Date;
            if (summary.WeekStart > today.AddDays(7) || summary.WeekEnd < today.AddDays(-31)) throw new InvalidOperationException("Choose a current, next or recent week (within 31 days).");
            var job = await db.Jobs.SingleOrDefaultAsync(j => j.JobId == actor.Job && j.IndividualID == actor.Person && j.OrganisationID == actor.Org &&
                j.JobStateId == 4 && j.JoinedDate <= summary.WeekEnd && (!j.TerminatedDate.HasValue || j.TerminatedDate >= summary.WeekStart))
                ?? throw new InvalidOperationException("Approved active employment is required.");
            if (await db.WeeklyHourExceptionRequests.AnyAsync(r => r.IndividualId == actor.Person && r.OrganisationId == actor.Org &&
                r.WeekStart == summary.WeekStart && !r.Request.RequestState.IsFinalState))
                throw new InvalidOperationException("You already have a pending exception for this week.");
            var type = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == TypeName)
                ?? throw new InvalidOperationException("Weekly exceptions are not available yet. Please contact HR.");
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE,
                $"Weekly hours exception submitted by user {actor.User}, tenant {actor.Org}, employee {actor.Person}, week {summary.WeekStart:yyyy-MM-dd}, {minutes} minutes.");
            var request = await requests.CreateAsync(db, new CreateRequestDto { RequestTypeId = type.RequestTypeId,
                ApplicantBusinessEntityId = actor.Person, OrganisationBusinessEntityId = actor.Org, OperationLogId = audit.OperationLogId, EffectiveDate = summary.WeekStart });
            db.WeeklyHourExceptionRequests.Add(new() { Request = request, RequestId = request.RequestId, JobId = job.JobId,
                OrganisationId = actor.Org, IndividualId = actor.Person, WeekStart = summary.WeekStart, ExcusedMinutes = minutes,
                Reason = reason.Trim(), CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync(); await workflow.StartAsync(db, request.RequestId, job.JobId); await tx.CommitAsync();
            return ServiceResult.Ok($"{request.ReferenceNumber} submitted for approval.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); if (ex is WorkflowConfigurationException config) await config.NotifyAsync(factory); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The request changed. Refresh and try again."); }
    }
    public async Task<List<WeeklyHourExceptionRow>> LoadAsync(bool pendingOnly)
    {
        var actor = await ScopeAsync(); await using var db = await factory.CreateDbContextAsync();
        var rows = await db.WeeklyHourExceptionRequests.AsNoTracking().Include(r => r.Request).ThenInclude(r => r.RequestState)
            .Where(r => r.OrganisationId == actor.Org && (!pendingOnly || r.Request.RequestState.IsProcessingState && !r.Request.RequestState.IsFinalState) &&
                (r.IndividualId == actor.Person || db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.User && a.RequestStageInstance.WorkflowRequestInstance.RequestId == r.RequestId)))
            .OrderByDescending(r => r.CreatedAtUtc).Take(100).ToListAsync();
        var result = new List<WeeklyHourExceptionRow>();
        foreach (var r in rows)
        {
            var instance = await WorkflowRequestProcessing.Instance(db, r.RequestId);
            var name = await db.Individuals.Where(i => i.BusinessEntityId == r.IndividualId).Select(i => i.FirstNameEnglish + " " + i.LastNameEnglish).SingleAsync();
            result.Add(new(r.RequestId, r.Request.RequestStateId, r.Request.ReferenceNumber ?? $"#{r.RequestId}", name,
                r.WeekStart, r.ExcusedMinutes, r.Reason, (r.Request.RequestState.IsProcessingState && !r.Request.RequestState.IsFinalState ? "Processing · " : "") + r.Request.RequestState.StateName,
                await Processing.CanApprove(db, Subject(r), instance, actor.User, actor.Person),
                r.IndividualId == actor.Person && !r.Request.RequestState.IsFinalState && instance?.WorkflowStatus == "InProgress"));
        }
        return result;
    }
    public async Task<ServiceResult> ActAsync(int id, int expectedState, string action, string? comment)
    {
        var actor = await ScopeAsync(); await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var r = await db.WeeklyHourExceptionRequests.Include(x => x.Request).ThenInclude(x => x.RequestState)
                .SingleOrDefaultAsync(x => x.RequestId == id && x.OrganisationId == actor.Org) ?? throw new UnauthorizedAccessException("Request unavailable in this organisation.");
            await Processing.ExecuteAsync(db, Subject(r), expectedState, action, comment, actor.User, actor.Person,
                async () =>
                {
                    var summary = await WeeklyWorkingHours.LoadAsync(db, r.IndividualId, r.OrganisationId, r.WeekStart);
                    if (!summary.HasWorkingHoursRule || r.ExcusedMinutes > Math.Round(summary.RequiredWeekHours * 60))
                        throw new InvalidOperationException("The weekly requirement changed. Reject or cancel this request and submit an updated exception.");
                }, () => Task.CompletedTask);
            await tx.CommitAsync(); return ServiceResult.Ok("Weekly hours exception action saved.");
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            if (ex is WorkflowConfigurationException configuration) await configuration.NotifyAsync(factory);
            if (ex is RequestTransitionConfigurationException transition)
            {
                var subject = await db.WeeklyHourExceptionRequests.AsNoTracking().SingleAsync(r => r.RequestId == id && r.OrganisationId == actor.Org);
                var context = await contextResolver.ResolveAsync(db, id, subject.JobId);
                await new WorkflowConfigurationException(context, transition.Detail) { ConfigurationUrl = "/admin/settings/config/state-transitions" }.NotifyAsync(factory);
            }
            return ServiceResult.Fail(ex.Message);
        }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The request changed. Refresh and try again."); }
    }
}
