using HRM.Application.Contracts.Pages;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Queries;

public sealed class AttendanceRequestQueries(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, ApiReadScope scope) : IAttendanceRequestQueries
{
    public Task<AttendanceRequestPage> GetMineAsync(AttendanceRequestFilter filter, CancellationToken ct = default) => SearchAsync(filter, true, ct);
    public Task<AttendanceRequestPage> GetReceivedAsync(AttendanceRequestFilter filter, CancellationToken ct = default) => SearchAsync(filter, false, ct);
    private async Task<AttendanceRequestPage> SearchAsync(AttendanceRequestFilter filter, bool mine, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        var organisation = await scope.OrganisationAsync(filter.OrganisationId);
        var (start, until) = ApiReadScope.Dates(filter.From, filter.To);
        if (mine && filter.StaffId.HasValue && filter.StaffId != actor.IndividualId) throw new UnauthorizedAccessException("Use your own requests.");
        var canViewAll = !mine && (actor.IsSuperAdministrator || await access.HasPermissionAsync("Attendance.View") || await access.HasPermissionAsync("Admin"));
        await using var db = await factory.CreateDbContextAsync(ct);
        var links = db.AttendanceLogRequests.Select(r => new { r.RequestId, r.AttendanceLogId, r.Comments, Kind = "Attendance" })
            .Concat(db.AttendanceLogChangeRequests.Select(r => new { r.RequestId, r.AttendanceLogId, r.Comments, Kind = "Uneven-log correction" }));
        var rows = await (from link in links
            join request in db.Requests on link.RequestId equals request.RequestId
            join log in db.AttendanceLogs on link.AttendanceLogId equals log.AttendanceLogId
            join state in db.RequestStates on request.RequestStateId equals state.RequestStateId
            join person in db.Individuals on log.IndividualId equals person.BusinessEntityId
            join org in db.Organisations on log.OrganisationId equals org.BusinessEntityID
            where log.OrganisationId == organisation && log.Date >= start && log.Date < until &&
                (!filter.StateId.HasValue || request.RequestStateId == filter.StateId) &&
                (!filter.StaffId.HasValue || log.IndividualId == filter.StaffId) &&
                (mine ? log.IndividualId == actor.IndividualId : canViewAll || db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.UserId && a.RequestStageInstance.WorkflowRequestInstance.RequestId == request.RequestId))
            orderby request.RequestId descending
            select new AttendanceRequestRow(request.RequestId, request.ReferenceNumber ?? "", link.Kind, log.Date,
                request.RequestStateId, state.StateName, (person.FirstNameEnglish + " " + (person.MiddleNameEnglish ?? "") + " " + (person.LastNameEnglish ?? "")).Trim(),
                org.OrganisationName, log.InOutMode.NameLong, link.Comments)).Take(1000).ToListAsync(ct);
        var states = await db.RequestStates.AsNoTracking().OrderBy(s => s.RequestStateId)
            .Select(s => new RequestStateOption(s.RequestStateId, s.StateName, s.IsProcessingState, s.IsFinalState)).ToListAsync(ct);
        return new(states, rows.DistinctBy(r => r.RequestId).ToList());
    }
}
