using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using HRM.Services.Interfaces;
using HRM.Helpers;

namespace HRM.WorkPlanning.Services;

public sealed class WorkforceScheduleReviewService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : IWorkforceScheduleReviewService
{
    public async Task<IReadOnlyList<WorkforceSessionReviewRow>> GetSessionsAsync(int organisationId, CancellationToken cancellationToken = default) =>
        (await GetSessionsPageAsync(organisationId, new(), cancellationToken)).Items;
    public async Task<IReadOnlyList<WorkforceLedgerReviewRow>> GetLedgerAsync(int organisationId, CancellationToken cancellationToken = default) =>
        (await GetLedgerPageAsync(organisationId, new(), cancellationToken)).Items;

    private async Task RequireAccessAsync(int org, string permission)
    {
        var actor = await access.RequireContextAsync();
        if (org <= 0 || org != await access.GetCurrentOrganisationIdAsync() || !await access.HasOrganisationAccessAsync(org) ||
            (!actor.IsSuperAdministrator && !await access.HasPermissionAsync(permission)))
            throw new UnauthorizedAccessException($"{permission} permission and access to the current organisation are required.");
    }
    private static (DateTime From, DateTime To) Window(WorkforceReviewQuery query)
    {
        var from = (query.From ?? DateTime.UtcNow.AddDays(-30)).Date;
        var to = (query.To ?? DateTime.UtcNow).Date;
        if (query.Page < 1 || query.Page > 100000 || query.PageSize is < 1 or > 100 || from > to || (to - from).TotalDays > 366 || to == DateTime.MaxValue.Date)
            throw new InvalidOperationException("Choose a date range up to 366 days, a positive page, and a page size between 1 and 100.");
        return (from, to.AddDays(1));
    }

    public async Task<WorkforceReviewPage<WorkforceSessionReviewRow>> GetSessionsPageAsync(int organisationId, WorkforceReviewQuery query, CancellationToken cancellationToken = default)
    {
        await RequireAccessAsync(organisationId, "Attendance.View");
        var window = Window(query);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var rows = from session in db.WorkforceAttendanceSessions.AsNoTracking()
                      join segment in db.WorkPlanSegments.AsNoTracking() on session.WorkPlanSegmentId equals segment.WorkPlanSegmentId
                      join plan in db.WorkPlans.AsNoTracking() on segment.WorkPlanId equals plan.WorkPlanId
                      where plan.OrganisationBusinessEntityId == organisationId && session.EvaluatedAtUtc >= window.From && session.EvaluatedAtUtc < window.To
                      select new { Session = session, plan.IndividualId };
        var visible = await StaffAttributeAccess.FilterViewableAsync(db, access, organisationId, await rows.Select(x => x.IndividualId).Distinct().ToListAsync(cancellationToken), cancellationToken);
        rows = rows.Where(x => visible.Contains(x.IndividualId));
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows.OrderByDescending(x => x.Session.EvaluatedAtUtc).ThenByDescending(x => x.Session.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new WorkforceSessionReviewRow(x.Session.Id, x.Session.WorkPlanSegmentId, x.Session.DutyAssignmentId, x.Session.Status, x.Session.EvaluationStrategy, x.Session.EvaluationVersion, x.Session.ActualMinutes, x.Session.CreditedMinutes, x.Session.MatchReason, x.Session.EvaluatedAtUtc)).ToListAsync(cancellationToken);
        return new(items, total, query.Page, query.PageSize);
    }

    public async Task<WorkforceReviewPage<WorkforceLedgerReviewRow>> GetLedgerPageAsync(int organisationId, WorkforceReviewQuery query, CancellationToken cancellationToken = default)
    {
        await RequireAccessAsync(organisationId, "Payroll.View");
        var window = Window(query);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var rows = db.WorkTimeLedgerEntries.AsNoTracking().Where(x => x.OrganisationId == organisationId && x.StartAt >= window.From && x.StartAt < window.To);
        var visible = await StaffAttributeAccess.FilterViewableAsync(db, access, organisationId, await rows.Select(x => x.IndividualId).Distinct().ToListAsync(cancellationToken), cancellationToken);
        rows = rows.Where(x => visible.Contains(x.IndividualId));
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows.OrderByDescending(x => x.StartAt).ThenByDescending(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new WorkforceLedgerReviewRow(x.Id, x.IndividualId, x.JobId, x.StartAt, x.EndAt, x.TimeCategory, x.ActualMinutes, x.CreditedMinutes, x.SourceReference, x.Status, x.PayrollExportReference, x.IsPayrollFinalized, x.CorrectsLedgerEntryId)).ToListAsync(cancellationToken);
        return new(items, total, query.Page, query.PageSize);
    }
}
