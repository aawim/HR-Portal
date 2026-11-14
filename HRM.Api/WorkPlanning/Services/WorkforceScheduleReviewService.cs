using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

public sealed class WorkforceScheduleReviewService(IDbContextFactory<HrmTeContext> factory) : IWorkforceScheduleReviewService
{
    public async Task<IReadOnlyList<WorkforceSessionReviewRow>> GetSessionsAsync(int organisationId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await (from session in db.WorkforceAttendanceSessions.AsNoTracking()
                      join segment in db.WorkPlanSegments.AsNoTracking() on session.WorkPlanSegmentId equals segment.WorkPlanSegmentId
                      join plan in db.WorkPlans.AsNoTracking() on segment.WorkPlanId equals plan.WorkPlanId
                      where plan.OrganisationBusinessEntityId == organisationId
                      orderby session.EvaluatedAtUtc descending
                      select new WorkforceSessionReviewRow(session.Id, session.WorkPlanSegmentId, session.DutyAssignmentId, session.Status, session.EvaluationStrategy, session.EvaluationVersion, session.ActualMinutes, session.CreditedMinutes, session.MatchReason, session.EvaluatedAtUtc)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkforceLedgerReviewRow>> GetLedgerAsync(int organisationId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.WorkTimeLedgerEntries.AsNoTracking().Where(x => x.OrganisationId == organisationId).OrderByDescending(x => x.StartAt)
            .Select(x => new WorkforceLedgerReviewRow(x.Id, x.IndividualId, x.JobId, x.StartAt, x.EndAt, x.TimeCategory, x.ActualMinutes, x.CreditedMinutes, x.SourceReference, x.Status, x.PayrollExportReference, x.IsPayrollFinalized, x.CorrectsLedgerEntryId)).ToListAsync(cancellationToken);
    }
}
