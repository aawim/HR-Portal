using HRM.Helpers;
using HRM.Models;
using HRM.Models.Workflow;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HRM.Services;




public sealed partial class RequestNotificationService(IDbContextFactory<HrmTeContext> factory, AuthenticationStateProvider authentication,
    HRM.Services.Interfaces.IUserAccessService? access = null)
:global::HRM.Services.Contracts.RequestNotificationServiceContracts,global::HRM.Services.Contracts.RequestNotificationServiceContracts.IOperations{
    private sealed record Actor(int UserId, int PersonId, int? OrganisationId);

    private async Task<Actor?> CurrentActorAsync(HrmTeContext db, CancellationToken token)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var username = principal.FindFirst("idnumber")?.Value;
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(username)) return null;
        var actor = await db.Users.AsNoTracking().Where(x => x.Username == username && x.IsActive && x.LocalUserStateId == 1)
            .Select(x => new Actor(x.UserId, x.BusinessEntityID, (int?)x.UserOrganisation!.BusinessEntityID)).SingleOrDefaultAsync(token);
        if(actor == null || access == null) return actor;
        var context = await access.RequireContextAsync();
        if(context.UserId != actor.UserId || context.IndividualId != actor.PersonId) return null;
        var org = await access.GetCurrentOrganisationIdAsync();
        return org > 0 && await access.HasOrganisationAccessAsync(org) ? actor with { OrganisationId = org } : null;
    }

    private static IQueryable<WfRequestApproval> PendingFor(HrmTeContext db, Actor actor)
        => db.WfRequestApprovals.Where(a => a.ApproverUserId == actor.UserId && a.Decision == "Pending" &&
            a.RequestStageInstance.StageStatus == "Pending" && a.RequestStageInstance.CompletedAt == null &&
            a.RequestStageInstance.WorkflowRequestInstance.WorkflowStatus == "InProgress" &&
            a.RequestStageInstance.WorkflowRequestInstance.CompletedAt == null &&
            a.RequestStageInstance.WorkflowRequestInstance.CurrentRequestStageInstanceId == a.RequestStageInstanceId &&
            (a.RequestStageInstance.WorkflowStage.ApprovalMode != "Sequential" ||
                !a.RequestStageInstance.Approvals.Any(other => other.Decision == "Pending" && other.ApprovalSequence < a.ApprovalSequence)) &&
            // Removed group membership must not continue to produce actionable alerts.
            (a.StageApprover == null ||
                (a.StageApprover.ApproverType != "UserGroup" && a.StageApprover.ApproverType != "SupervisorGroup") ||
                db.UserAssignedUserGroups.Any(g => g.UserId == actor.UserId && g.UserGroupId == a.StageApprover.ApproverReferenceId &&
                    (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisation.BusinessEntityID == actor.OrganisationId))))
            .Where(a => db.Requests.Any(r => r.RequestId == a.RequestStageInstance.WorkflowRequestInstance.RequestId &&
                !r.RequestState.IsFinalState &&
                (r.OrganisationBusinessEntityID == null || r.OrganisationBusinessEntityID == actor.OrganisationId) &&
                r.ApplicantBusinessEntityId != actor.PersonId &&
                !db.LeaveRequests.Any(l => l.RequestId == r.RequestId && l.Leave.Job.IndividualID == actor.PersonId) &&
                !db.LeaveChangeRequests.Any(l => l.RequestId == r.RequestId && l.Leave.Job.IndividualID == actor.PersonId)));

    private static IQueryable<RequestNotification> Owned(HrmTeContext db, Actor actor, List<HandoverAlert> handovers)
    {
        var pending = PendingFor(db, actor).Select(a => a.RequestStageInstanceId);
        var tasks = handovers.Where(x => x.Kind == "HandoverTasks").Select(x => x.LeaveId).ToArray();
        var assigned = handovers.Where(x => x.Kind == "HandoverAssigned").Select(x => x.LeaveId).ToArray();
        var reviews = handovers.Where(x => x.Kind == "HandoverReview").Select(x => x.LeaveId).ToArray();
        var returned = handovers.Where(x => x.Kind == "HandoverReturned").Select(x => x.LeaveId).ToArray();
        var owned = db.RequestNotifications.Where(n => n.UserId == actor.UserId &&
            (n.NotificationKind != "DutyAcceptance" || n.Request.RequestStateId == 10) &&
            (n.Request.OrganisationBusinessEntityID == null || n.Request.OrganisationBusinessEntityID == actor.OrganisationId) &&
            (n.NotificationKind != "ActionRequired" || (n.RequestStageInstanceId.HasValue && pending.Contains(n.RequestStageInstanceId.Value))) &&
            (!n.NotificationKind.StartsWith("Handover") || (n.LeaveId.HasValue &&
                db.LeaveHandoverReviews.Any(r => r.LeaveId == n.LeaveId && r.Revision == n.HandoverRevision) &&
                ((n.NotificationKind == "HandoverTasks" && tasks.Contains(n.LeaveId.Value)) ||
                 (n.NotificationKind == "HandoverAssigned" && assigned.Contains(n.LeaveId.Value)) ||
                 (n.NotificationKind == "HandoverReview" && reviews.Contains(n.LeaveId.Value)) ||
                 (n.NotificationKind == "HandoverReturned" && returned.Contains(n.LeaveId.Value))))));
        if(db.Model.FindEntityType(typeof(HRM.Models.WorkPlanning.DutyChangeRequest)) != null)
        {
            var now = DateTime.UtcNow.AddHours(5);
            owned = owned.Where(n => n.NotificationKind != "DutyAcceptance" || db.DutyChangeRequests.Any(d => d.RequestId == n.RequestId && d.IsOpen &&
                d.SourceStart > now && (!d.SwapStart.HasValue || d.SwapStart > now) &&
                ((d.SourceIndividualId == actor.PersonId && d.SourceAcceptedAtUtc == null) || (d.TargetIndividualId == actor.PersonId && d.AcceptedAtUtc == null))));
        }
        return owned;
    }

    private static async Task EnsureActionNotificationsAsync(HrmTeContext db, Actor actor, CancellationToken token)
    {
        // Reconcile the actual current inbox, including requests assigned before this feature.
        // No broadcast to everyone with a Supervisor/HR role: only assigned users get an alert.
        var candidates = await (from a in PendingFor(db, actor)
                                join r in db.Requests on a.RequestStageInstance.WorkflowRequestInstance.RequestId equals r.RequestId
                                where !db.RequestNotifications.Any(n => n.UserId == actor.UserId &&
                                    n.NotificationKind == "ActionRequired" && n.RequestStageInstanceId == a.RequestStageInstanceId)
                                select new { a.RequestStageInstanceId, r.RequestId, r.ReferenceNumber, r.RequestStateId })
                                .Distinct().ToListAsync(token);
        if (candidates.Count == 0) return;
        foreach (var row in candidates)
            db.RequestNotifications.Add(new RequestNotification { UserId = actor.UserId, RequestId = row.RequestId,
                NotificationKind = "ActionRequired", RequestStageInstanceId = row.RequestStageInstanceId,
                FromStateId = row.RequestStateId, ToStateId = row.RequestStateId,
                ReferenceNumber = row.ReferenceNumber ?? $"#{row.RequestId}", CreatedAtUtc = DateTime.UtcNow });
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Two open tabs can discover the assignment together. The unique index keeps one alert.
            foreach (var entry in db.ChangeTracker.Entries<RequestNotification>().Where(x => x.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;
        }
    }

    public async Task<RequestNotificationInbox> GetInboxAsync(CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var actor = await CurrentActorAsync(db, token);
        if (actor == null) return new(0, []);
        await EnsureActionNotificationsAsync(db, actor, token);
        await EnsureDutyAcceptanceNotificationsAsync(db, actor, token);
        var handovers = await LoadHandoverAlertsAsync(db, actor, token);
        await EnsureHandoverNotificationsAsync(db, actor, handovers, token);
        var owned = Owned(db, actor, handovers);
        var unread = await owned.CountAsync(x => x.ReadAtUtc == null, token);
        var rows = await (from n in owned.AsNoTracking()
                          join fromState in db.RequestStates on n.FromStateId equals fromState.RequestStateId
                          join toState in db.RequestStates on n.ToStateId equals toState.RequestStateId
                          orderby (n.ReadAtUtc == null) descending, n.CreatedAtUtc descending, n.RequestNotificationId descending
                              select new { n.RequestNotificationId, ReferenceNumber = n.Request.ReferenceNumber ?? n.ReferenceNumber, n.CreatedAtUtc, n.ReadAtUtc,
                              n.NotificationKind, n.Request.RequestId, n.LeaveId, n.Request.RequestType.ViewUrl, RequestTypeName = n.Request.RequestType.TypeName,
                              ActionStage = db.WfRequestStageInstances.Where(s => s.RequestStageInstanceId == n.RequestStageInstanceId)
                                  .Select(s => s.WorkflowStage.StageName).FirstOrDefault(),
                              FromState = fromState.StateName, ToState = toState.StateName, n.Request.RequestTypeId,
                              IsLeave = db.LeaveRequests.Any(x => x.RequestId == n.RequestId),
                              IsStaffPosition = db.StaffPositionChangeRequests.Any(x=>x.RequestId==n.RequestId),
                              IsAttendance = n.Request.AttendanceLogChangeRequest != null || n.Request.AttendanceLogRequests.Any() })
                          .Take(30).ToListAsync(token);
        var requestItems = rows.Select(n => new RequestNotificationItem(n.RequestNotificationId, n.ReferenceNumber,
            n.FromState.Trim(), n.ToState.Trim(), n.CreatedAtUtc, n.ReadAtUtc.HasValue,
            HRM.Application.Presentation.OvertimeNavigation.NotificationUrl(n.NotificationKind, n.RequestTypeName, n.ViewUrl, n.RequestId) ?? (
            n.IsStaffPosition ? StaffPositionManagementService.RequestsPageUrl :
            n.ViewUrl == StaffFinanceService.PageUrl ? (n.NotificationKind == "ActionRequired" ? StaffFinanceService.AdminUrl : StaffFinanceService.PageUrl) + $"?requestId={n.RequestId}" :
            n.ViewUrl == LateCheckInRequestService.PageUrl ? LateCheckInRequestService.PageUrl :
            n.ViewUrl == LateCheckInRequestService.EarlyPageUrl ? LateCheckInRequestService.EarlyPageUrl :
            n.ViewUrl == OvertimeRequestService.PageUrl ? OvertimeRequestService.PageUrl :
            n.ViewUrl == SupervisorChangeRequestService.PageUrl ? SupervisorChangeRequestService.PageUrl :
            n.ViewUrl == StaffPositionManagementService.RequestsPageUrl ? StaffPositionManagementService.RequestsPageUrl :
            n.ViewUrl == WeeklyHourExceptionService.PageUrl ? WeeklyHourExceptionService.PageUrl :
            n.ViewUrl == "/online/duty-roster" ? "/online/duty-roster" :
            n.ViewUrl == AdminAccessGrantService.PageUrl ? "/online/admin-access" :
            n.ViewUrl == StaffOnboardingService.PageUrl ? $"{StaffOnboardingService.PageUrl}?requestId={n.RequestId}" :
            n.NotificationKind.StartsWith("Handover") ? $"/online/leave-handovers/{n.LeaveId}" :
            n.NotificationKind == "ActionRequired" && n.IsAttendance ? HRM.Application.Presentation.AttendanceRequestNavigation.ReviewUrl(n.RequestId) :
            n.NotificationKind == "ActionRequired" ? $"/admin/my/workspace?requestId={n.RequestId}" :
            LeaveChangeRequestTypes.IsChange(n.RequestTypeId) ? "/online/leaves/leave-change-request" :
            n.IsLeave ? "/online/leaves/all-requests" : n.IsAttendance ? "/online/attendance/attendance-request" : "/online"),
            n.NotificationKind switch {
                "ActionRequired" => n.ActionStage ?? "Review request",
                "HandoverTasks" => "Verify your work handover tasks",
                "HandoverReview" => "Review and approve work handover",
                "HandoverReturned" => "Revise your work handover",
                _ => null },
            n.NotificationKind switch {
                "HandoverAssigned" => "Work handover assigned for your supervision. Employee verification is pending.",
                "RequestSubmitted" => "Your request has been submitted.",
                "RequestAction" => "An action was recorded on a request related to you. Open it to view the current progress.",
                "DutyAcceptance" => "A duty transfer or swap needs your acceptance. Open the roster to review it.",
                "DutyUpdate" => "Your duty request has been updated. Open the roster for its current status.",
                "DutyAccepted" => "A staff member accepted the duty change. Both staff must accept before supervisor approval.",
                "DutyDeclined" => "A staff member declined the duty change. The roster has not changed.",
                "DutyApproved" => "The duty change was approved. The roster has been updated.",
                "DutyRejected" => "The duty change was rejected. The roster has not changed.",
                "DutyCancelled" => "The duty change was cancelled. The roster has not changed.",
                "DutyVote" => "An approval was recorded for your duty change. Further approval is still pending.",
                "DutyReadyForApproval" => "Both staff members accepted the duty change. Supervisor approval is now required.",
                "DutySubmitted" => "Your duty change request was submitted. Both staff members have been notified to accept it.",
                _ => null })).ToList();
        var staff = await LoadStaffInboxAsync(db, actor, token);
        return new(unread + staff.UnreadCount, requestItems.Concat(staff.Items).OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Take(30).ToList());
    }

    public async Task MarkReadAsync(long? notificationId = null, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var actor = await CurrentActorAsync(db, token);
        if (actor == null) return;
        var owned = Owned(db, actor, await LoadHandoverAlertsAsync(db, actor, token));
        var items = await owned.Where(x => x.ReadAtUtc == null && (!notificationId.HasValue || x.RequestNotificationId == notificationId.Value)).ToListAsync(token);
        foreach (var item in items) item.ReadAtUtc = DateTime.UtcNow;
        await MarkStaffReadAsync(db, actor, notificationId, token);
        await db.SaveChangesAsync(token);
    }
}
