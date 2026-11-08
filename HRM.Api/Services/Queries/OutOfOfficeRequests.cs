using System.Data;
using HRM.Application.Contracts.Pages;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Queries;

public sealed class OutOfOfficeRequests(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, ApiReadScope scope,
    IRequestService requests, IOperationLogService logs, IWorkflowEngine workflow,
    IWorkflowApproverResolver approvers, IWorkflowRequestContextResolver contexts) : IOutOfOfficeRequests
{
    public async Task<OutOfOfficeOptions> GetOptionsAsync(CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync(); var org = await scope.OrganisationAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        return new(await db.Organisations.AsNoTracking().Where(o => actor.IsSuperAdministrator || o.BusinessEntityID == org)
                .OrderBy(o => o.OrganisationName).Select(o => new DirectoryOption(o.BusinessEntityID, o.OrganisationName)).ToListAsync(ct),
            await db.Teams.AsNoTracking().Where(t => t.OrganisationId == org && t.IsValid == true).OrderBy(t => t.Name).Select(t => new DirectoryOption(t.TeamId, t.Name)).ToListAsync(ct),
            await db.RequestStates.AsNoTracking().OrderBy(s => s.RequestStateId).Select(s => new RequestStateOption(s.RequestStateId, s.StateName, s.IsProcessingState, s.IsFinalState)).ToListAsync(ct),
            await db.OutOfficeRequestTypes.AsNoTracking().OrderBy(t => t.TypeName).Select(t => new DirectoryOption(t.OutOfOfficeRequestTypeId, t.TypeName)).ToListAsync(ct));
    }
    public async Task<OutOfOfficeTeamMembers> GetTeamMembersAsync(int teamId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync(); var org = await scope.OrganisationAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var today = DateTime.UtcNow.AddHours(5).Date;
        if (!await db.Teams.AnyAsync(t => t.TeamId == teamId && t.OrganisationId == org && t.IsValid == true, ct)) throw new UnauthorizedAccessException("Select a team in your organisation.");
        var mayRead = actor.IsSuperAdministrator || await access.HasPermissionAsync("Employee.View") || await access.HasPermissionAsync("Admin") ||
            await db.TeamStaffs.AnyAsync(t => t.TeamId == teamId && t.StaffId == actor.IndividualId && t.IsValid && t.StartDate <= today && (!t.EndDate.HasValue || t.EndDate > today), ct);
        if (!mayRead) throw new UnauthorizedAccessException("This team is unavailable.");
        var members = await db.TeamStaffs.AsNoTracking().Where(t => t.TeamId == teamId && t.IsValid && t.StartDate <= today && (!t.EndDate.HasValue || t.EndDate > today) &&
            db.Jobs.Any(j => j.IndividualID == t.StaffId && j.OrganisationID == org && j.JobStateId == 4 && j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate > today)))
            .Select(t => new { t.StaffId, t.IsSuperVisor, Name = (t.Staff.Individual.FirstNameEnglish + " " + (t.Staff.Individual.LastNameEnglish ?? "")).Trim() }).ToListAsync(ct);
        return new(members.DistinctBy(m => m.StaffId).Select(m => new DirectoryOption(m.StaffId, m.Name)).ToList(),
            members.Where(m => m.IsSuperVisor).DistinctBy(m => m.StaffId).Select(m => new DirectoryOption(m.StaffId, m.Name)).ToList());
    }
    public Task<List<OutOfOfficeRequestRow>> GetMineAsync(OutOfOfficeFilter filter, CancellationToken ct = default) => SearchAsync(filter, true, ct);
    public Task<List<OutOfOfficeRequestRow>> GetReceivedAsync(OutOfOfficeFilter filter, CancellationToken ct = default) => SearchAsync(filter, false, ct);
    private async Task<List<OutOfOfficeRequestRow>> SearchAsync(OutOfOfficeFilter filter, bool mine, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync(); var org = await scope.OrganisationAsync(filter.OrganisationId);
        var (from, until) = ApiReadScope.Dates(filter.From, filter.To);
        if (mine && filter.StaffId.HasValue && filter.StaffId != actor.IndividualId) throw new UnauthorizedAccessException("Use your own requests.");
        var readAll = !mine && (actor.IsSuperAdministrator || await access.HasPermissionAsync("Attendance.View") || await access.HasPermissionAsync("Admin"));
        await using var db = await factory.CreateDbContextAsync(ct);
        if (filter.TeamId.HasValue && !await db.Teams.AnyAsync(t => t.TeamId == filter.TeamId && t.OrganisationId == org, ct)) throw new UnauthorizedAccessException("Select a team in your organisation.");
        var rows = await db.OutOfOfficeRequests.AsNoTracking().Where(r => r.OrganisationBusinessEntityID == org && r.Date >= from && r.Date < until &&
                (!filter.StateId.HasValue || r.Request.RequestStateId == filter.StateId) && (!filter.StaffId.HasValue || r.StaffId == filter.StaffId) &&
                (!filter.TeamId.HasValue || db.TeamStaffs.Any(t => t.TeamId == filter.TeamId && t.StaffId == r.StaffId && t.StartDate <= r.Date && (!t.EndDate.HasValue || t.EndDate > r.Date))) &&
                (mine ? r.StaffId == actor.IndividualId : readAll || db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.UserId && a.RequestStageInstance.WorkflowRequestInstance.RequestId == r.RequestId)))
            .OrderByDescending(r => r.RequestId).Take(1000).Select(r => new
            {
                r.RequestId, Reference = r.Request.ReferenceNumber, State = r.Request.RequestStateId, Status = r.Request.RequestState.StateName, r.StaffId,
                Name = (r.Staff.Individual.FirstNameEnglish + " " + (r.Staff.Individual.LastNameEnglish ?? "")).Trim(),
                Supervisor = r.SupervisorStaff.Individual.FirstNameEnglish + " " + (r.SupervisorStaff.Individual.LastNameEnglish ?? ""),
                r.Date, r.ExpectedClockOutTime, r.ExpectedClockInTime, r.Remarks,
                Out = db.AttendanceLogs.Where(l => l.AttendanceLogId == r.ClockOutAttendanceLogId && l.IndividualId == r.StaffId && l.OrganisationId == org).Select(l => (DateTime?)l.Date).FirstOrDefault(),
                In = db.AttendanceLogs.Where(l => l.AttendanceLogId == r.ClockInAttendanceLogId && l.IndividualId == r.StaffId && l.OrganisationId == org).Select(l => (DateTime?)l.Date).FirstOrDefault()
            }).ToListAsync(ct);
        return rows.Select(r => new OutOfOfficeRequestRow(r.RequestId, r.Reference ?? $"#{r.RequestId}", r.State, r.Status, r.StaffId, r.Name, r.Supervisor,
            DateOnly.FromDateTime(r.Date), r.ExpectedClockOutTime, r.ExpectedClockInTime, r.Out, r.In, r.Remarks)).ToList();
    }
    public async Task<OutOfOfficeApplication?> GetEditableAsync(int requestId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync(); var org = await scope.OrganisationAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.OutOfOfficeRequests.AsNoTracking().Where(r => r.RequestId == requestId && r.OrganisationBusinessEntityID == org && r.StaffId == actor.IndividualId &&
            r.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE && !db.WfRequestInstances.Any(w => w.RequestId == r.RequestId)).SingleOrDefaultAsync(ct);
        return row is null ? null : new() { RequestId = row.RequestId, ExpectedStateId = SharedConfig.RequestStates.INCOMPLETE, RequestTypeId = row.OutOfOfficeRequestTypeId,
            Date = DateOnly.FromDateTime(row.Date), ExpectedOut = row.ExpectedClockOutTime, ExpectedIn = row.ExpectedClockInTime, Remarks = row.Remarks };
    }
    public async Task<ServiceResult> SubmitAsync(OutOfOfficeApplication application, CancellationToken ct = default)
    {
        if (application.Date == default || application.Date == DateOnly.MaxValue || application.ExpectedIn <= application.ExpectedOut || string.IsNullOrWhiteSpace(application.Remarks) || application.Remarks.Trim().Length > 2000)
            throw new ArgumentException("Enter a date, a return time after departure and a reason of up to 2,000 characters.");
        var actor = await access.RequireContextAsync(); var org = await scope.OrganisationAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var job = await LeaveApplicantScope.ResolveAsync(db, access, application.ApplicantJobId);
        if (job.OrganisationID != org || job.JobStateId != 4 || job.JoinedDate.Date > application.Date.ToDateTime(TimeOnly.MinValue) ||
            job.TerminatedDate.HasValue && job.TerminatedDate.Value.Date <= DateTime.UtcNow.AddHours(5).Date) throw new UnauthorizedAccessException("Use an active employment record in your organisation.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // The selected category is not the central RequestTypeID. The established workflow request type is 50.
            if (!await db.OutOfficeRequestTypes.AnyAsync(t => t.OutOfOfficeRequestTypeId == application.RequestTypeId, ct) ||
                !await db.RequestTypes.AnyAsync(t => t.RequestTypeId == SharedConfig.RequestTypes.OUT_OF_OFFICE_REQUEST, ct)) throw new InvalidOperationException("The out-of-office request configuration is unavailable.");
            OutOfOfficeRequest? row = null;
            if (application.RequestId.HasValue)
            {
                row = await db.OutOfOfficeRequests.Include(r => r.Request).SingleOrDefaultAsync(r => r.RequestId == application.RequestId && r.StaffId == job.IndividualID && r.OrganisationBusinessEntityID == org, ct)
                    ?? throw new UnauthorizedAccessException("Request unavailable.");
                if (application.ExpectedStateId != row.Request.RequestStateId || row.Request.RequestStateId != SharedConfig.RequestStates.INCOMPLETE || await db.WfRequestInstances.AnyAsync(w => w.RequestId == row.RequestId, ct))
                    throw new InvalidOperationException("Only an unchanged draft can be submitted here. Active and completed workflows cannot be overwritten.");
            }
            var day = application.Date.ToDateTime(TimeOnly.MinValue);
            if (await db.OutOfOfficeRequests.AnyAsync(r => r.StaffId == job.IndividualID && r.OrganisationBusinessEntityID == org && r.Date == day &&
                r.RequestId != (application.RequestId ?? 0) && r.Request.RequestStateId != 5 && r.Request.RequestStateId != 6 &&
                r.ExpectedClockOutTime < application.ExpectedIn && r.ExpectedClockInTime > application.ExpectedOut, ct)) throw new InvalidOperationException("Another request overlaps this time.");
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.OUT_OF_OFFICE_REQUEST_CREATE, "Submitted an out-of-office request.");
            var request = row?.Request ?? await requests.CreateAsync(db, new() { RequestTypeId = SharedConfig.RequestTypes.OUT_OF_OFFICE_REQUEST, ApplicantBusinessEntityId = job.IndividualID,
                OrganisationBusinessEntityId = org, OperationLogId = audit.OperationLogId, EffectiveDate = day });
            if (request.RequestTypeId != SharedConfig.RequestTypes.OUT_OF_OFFICE_REQUEST || request.ApplicantBusinessEntityId != job.IndividualID || request.OrganisationBusinessEntityID != org)
                throw new InvalidOperationException("This draft is not linked to the selected employee request.");
            if (!await db.RequestJobs.AnyAsync(j => j.RequestId == request.RequestId && j.IsValid, ct))
                db.RequestJobs.Add(new() { RequestId = request.RequestId, JobId = job.JobId, IsValid = true, OperationLogId = audit.OperationLogId });
            else if (!await db.RequestJobs.AnyAsync(j => j.RequestId == request.RequestId && j.IsValid && j.JobId == job.JobId, ct))
                throw new InvalidOperationException("This draft belongs to a different employment record.");
            request.RequestEffectiveDate = day;
            await db.SaveChangesAsync(ct);
            var instanceId = await workflow.StartAsync(db, request.RequestId, job.JobId, ct);
            var supervisor = await (from approval in db.WfRequestApprovals
                join user in db.Users on approval.ApproverUserId equals user.UserId
                join staff in db.Staffs on user.BusinessEntityID equals staff.IndividualId
                where approval.RequestStageInstance.WorkflowRequestInstanceId == instanceId && staff.IndividualId != job.IndividualID
                orderby approval.ApprovalSequence, approval.RequestApprovalId
                select (int?)staff.IndividualId).FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("The configured approval stage needs a staff approver.");
            row ??= new OutOfOfficeRequest { RequestId = request.RequestId, Request = request, OrganisationBusinessEntityID = org, StaffId = job.IndividualID };
            row.SupervisorStaffId = supervisor; row.Date = day; row.ExpectedClockOutTime = application.ExpectedOut; row.ExpectedClockInTime = application.ExpectedIn;
            row.Remarks = application.Remarks.Trim(); row.OutOfOfficeRequestTypeId = application.RequestTypeId; row.OrganisationStructureId = job.OrganisationStructureId;
            if (!application.RequestId.HasValue) db.OutOfOfficeRequests.Add(row);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return ServiceResult.Ok($"{request.ReferenceNumber} submitted for approval.");
        }
        catch (WorkflowConfigurationException ex) { await tx.RollbackAsync(CancellationToken.None); await ex.NotifyAsync(factory); return ServiceResult.Failed(ex.Message); }
    }
    public async Task<ServiceResult> ActAsync(int requestId, int expectedState, string action, string? comment, CancellationToken ct = default)
    {
        if (action is not ("Approve" or "Reject" or "Cancel")) throw new ArgumentException("Select a valid request action.");
        var actor = await access.RequireContextAsync(); var org = await scope.OrganisationAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var row = await db.OutOfOfficeRequests.Include(r => r.Request).ThenInclude(r => r.RequestState)
                .SingleOrDefaultAsync(r => r.RequestId == requestId && r.OrganisationBusinessEntityID == org, ct)
                ?? throw new UnauthorizedAccessException("Request unavailable in this organisation.");
            if (action == "Cancel" ? row.StaffId != actor.IndividualId : row.StaffId == actor.IndividualId)
                throw new UnauthorizedAccessException("Only the applicant can cancel, and applicants cannot approve or reject their own requests.");
            if (row.Request.RequestTypeId != SharedConfig.RequestTypes.OUT_OF_OFFICE_REQUEST || row.Request.OrganisationBusinessEntityID != org || row.Request.ApplicantBusinessEntityId != row.StaffId)
                throw new InvalidOperationException("The legacy request is not correctly linked to this employee and organisation.");
            var jobs = await db.RequestJobs.Where(j => j.RequestId == requestId && j.IsValid && j.Job.IndividualID == row.StaffId && j.Job.OrganisationID == org)
                .Select(j => j.JobId).Distinct().ToListAsync(ct);
            if (jobs.Count != 1) throw new InvalidOperationException("The request needs an unambiguous employment link before it can be processed.");
            await new WorkflowRequestProcessing(approvers, contexts).ExecuteAsync(db, new(row.Request, jobs[0], row.StaffId, org),
                expectedState, action, comment, actor.UserId, actor.IndividualId, () => Task.CompletedTask, () => Task.CompletedTask);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return ServiceResult.Ok("Request action saved.");
        }
        catch (WorkflowConfigurationException ex) { await tx.RollbackAsync(CancellationToken.None); await ex.NotifyAsync(factory); return ServiceResult.Failed(ex.Message); }
        catch (RequestTransitionConfigurationException ex)
        {
            await tx.RollbackAsync(CancellationToken.None);
            var jobs = await db.RequestJobs.AsNoTracking().Where(j => j.RequestId == requestId && j.IsValid).Select(j => j.JobId).ToListAsync(ct);
            if (jobs.Count == 1)
            {
                var context = await contexts.ResolveAsync(db, requestId, jobs[0], ct);
                await new WorkflowConfigurationException(context, ex.Detail) { ConfigurationUrl = "/admin/settings/config/state-transitions" }.NotifyAsync(factory);
            }
            return ServiceResult.Failed(ex.Message);
        }
    }

}
