using HRM.Application.Contracts.Pages;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Queries;

public sealed class LeaveRequestWorkflowQueries(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : ILeaveRequestWorkflowQueries
{
    public async Task<List<RequestStateOption>> GetStatesAsync(CancellationToken ct = default)
    {
        await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.RequestStates.AsNoTracking().OrderBy(s => s.RequestStateId)
            .Select(s => new RequestStateOption(s.RequestStateId, s.StateName, s.IsProcessingState, s.IsFinalState)).ToListAsync(ct);
    }
    // Leave-record filter IDs are distinct from workflow RequestState IDs, especially Rejected/Cancelled.
    public async Task<List<DirectoryOption>> GetLeaveStatesAsync(CancellationToken ct = default)
    {
        await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.LeaveStates.AsNoTracking().OrderBy(s => s.LeaveStateId).Select(s => new DirectoryOption(s.LeaveStateId, s.StateName)).ToListAsync(ct);
    }
    public async Task<List<LeaveRequestWorkflowSummary>> GetMineAsync(IReadOnlyList<int> leaveIds, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (leaveIds.Count > 1000 || leaveIds.Any(id => id <= 0)) throw new ArgumentException("Select up to 1,000 leave records.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var mappings = await db.LeaveRequests.AsNoTracking().Where(l => leaveIds.Contains(l.LeaveId) && l.Leave.Job.IndividualID == actor.IndividualId)
            .Select(l => new { l.LeaveId, l.RequestId, Reference = l.Request.ReferenceNumber, State = l.Request.RequestStateId,
                Status = l.Request.RequestState.StateName, Processing = l.Request.RequestState.IsProcessingState, Final = l.Request.RequestState.IsFinalState,
                Reason = l.Request.StateChangeRemarks, Handover = db.LeaveHandoverReviews.Any(h => h.LeaveId == l.LeaveId) }).ToListAsync(ct);
        var requests = mappings.Select(m => m.RequestId).ToArray();
        var workflows = await db.WfRequestInstances.AsNoTracking().Where(w => requests.Contains(w.RequestId))
            .Select(w => new { w.RequestId, Id = w.WorkflowRequestInstanceId, Status = w.WorkflowStatus,
                Stage = w.CurrentStage != null ? w.CurrentStage.WorkflowStage.StageName : null, Completed = w.CompletedAt.HasValue }).ToListAsync(ct);
        var latest = workflows.GroupBy(w => w.RequestId).ToDictionary(g => g.Key, g => g.OrderByDescending(w => w.Id).First());
        return mappings.Select(m =>
        {
            latest.TryGetValue(m.RequestId, out var workflow);
            return new LeaveRequestWorkflowSummary(m.LeaveId, m.RequestId, m.Reference ?? $"#{m.RequestId}", m.State,
                m.Status, m.Processing && !m.Final, m.Final, m.Reason, m.Handover,
                workflow?.Id, workflow?.Status, workflow?.Stage, workflow?.Completed ?? false);
        }).ToList();
    }
}
