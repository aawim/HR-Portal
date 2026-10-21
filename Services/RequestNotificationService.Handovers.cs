using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Leave;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed partial class RequestNotificationService
{
    private sealed record HandoverAlert(string Kind, int LeaveId, int Revision, int RequestId, int StateId, string Reference);

    private static async Task<List<HandoverAlert>> LoadHandoverAlertsAsync(HrmTeContext db, Actor actor, CancellationToken token)
    {
        var candidates = await (from link in db.LeaveRequests.AsNoTracking()
                                join review in db.LeaveHandoverReviews on link.LeaveId equals review.LeaveId
                                where !link.Request.RequestState.IsFinalState &&
                                    link.Leave.Job.OrganisationID == actor.OrganisationId &&
                                    link.Leave.LeaveStateId != SharedConfig.LeaveStates.APPROVED &&
                                    link.Leave.LeaveStateId != SharedConfig.LeaveStates.CANCELLED &&
                                    link.Leave.LeaveStateId != SharedConfig.LeaveStates.REJECTED
                                select new { link.LeaveId, link.RequestId, link.Request.ReferenceNumber, link.Request.RequestStateId,
                                    review.Revision, review.SupervisorDecision, link.Leave.JobId,
                                    IsApplicant = link.Leave.Job.IndividualID == actor.PersonId,
                                    HasPendingTasks = db.LeaveWorkHandOvers.Any(t => t.LeaveId == link.LeaveId && t.IsValid &&
                                        t.Decision == "Pending" && t.Job.IndividualID == actor.PersonId),
                                    AssignedSupervisor = db.LeaveHandoverSupervisors.Any(s => s.LeaveId == link.LeaveId && s.IndividualId == actor.PersonId),
                                    TasksReturned = db.LeaveWorkHandOvers.Any(t => t.LeaveId == link.LeaveId && t.IsValid && t.Decision == "Rejected"),
                                    AllTasksApproved = db.LeaveWorkHandOvers.Any(t => t.LeaveId == link.LeaveId && t.IsValid) &&
                                        !db.LeaveWorkHandOvers.Any(t => t.LeaveId == link.LeaveId && t.IsValid && t.Decision != "Approved") })
                                .Where(x => x.IsApplicant || x.HasPendingTasks || x.AssignedSupervisor).ToListAsync(token);
        var alerts = new List<HandoverAlert>();
        foreach (var item in candidates)
        {
            void Add(string kind) => alerts.Add(new(kind, item.LeaveId, item.Revision, item.RequestId, item.RequestStateId,
                item.ReferenceNumber ?? $"#{item.RequestId}"));
            if (item.IsApplicant)
            {
                if (item.SupervisorDecision == "Rejected" || item.TasksReturned) Add("HandoverReturned");
                continue;
            }
            if (item.SupervisorDecision != "Pending") continue;
            if (item.HasPendingTasks) Add("HandoverTasks");
            if (item.AssignedSupervisor && (await LeaveHandoverService.SupervisorsAsync(db, item.JobId)).Contains(actor.PersonId))
                Add(item.AllTasksApproved ? "HandoverReview" : "HandoverAssigned");
        }
        return alerts;
    }

    private static async Task EnsureHandoverNotificationsAsync(HrmTeContext db, Actor actor, List<HandoverAlert> alerts, CancellationToken token)
    {
        if (alerts.Count == 0) return;
        var leaveIds = alerts.Select(x => x.LeaveId).Distinct().ToArray();
        var existing = await db.RequestNotifications.AsNoTracking().Where(n => n.UserId == actor.UserId &&
            n.LeaveId.HasValue && leaveIds.Contains(n.LeaveId.Value) && n.NotificationKind.StartsWith("Handover"))
            .Select(n => new { n.NotificationKind, n.LeaveId, n.HandoverRevision }).ToListAsync(token);
        var added = false;
        foreach (var alert in alerts)
        {
            if (existing.Any(n => n.NotificationKind == alert.Kind && n.LeaveId == alert.LeaveId && n.HandoverRevision == alert.Revision)) continue;
            db.RequestNotifications.Add(new RequestNotification { UserId = actor.UserId, RequestId = alert.RequestId,
                NotificationKind = alert.Kind, LeaveId = alert.LeaveId, HandoverRevision = alert.Revision,
                FromStateId = alert.StateId, ToStateId = alert.StateId, ReferenceNumber = alert.Reference, CreatedAtUtc = DateTime.UtcNow });
            added = true;
        }
        if (!added) return;
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            foreach (var entry in db.ChangeTracker.Entries<RequestNotification>().Where(x => x.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;
        }
    }
}
