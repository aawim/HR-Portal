using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;
public sealed partial class RequestNotificationService
{
    private static async Task<RequestNotificationInbox> LoadStaffInboxAsync(HrmTeContext db, Actor actor, CancellationToken ct)
    {
        if(db.Model.FindEntityType(typeof(StaffNotification)) == null) return new(0, []);
        var query = db.StaffNotifications.AsNoTracking().Where(x => x.UserId == actor.UserId && x.IndividualId == actor.PersonId && x.OrganisationId == actor.OrganisationId);
        if(db.Model.FindEntityType(typeof(StaffChatMembership)) != null)
            query = query.Where(n => (n.Kind != "Chat" && n.Kind != "ChatMembership") || n.Url == StaffChatService.PageUrl ||
                db.StaffChatMemberships.Any(m => m.UserId == actor.UserId && m.LeftAtUtc == null &&
                    m.Conversation.OrganisationId == actor.OrganisationId && n.Url == StaffChatService.PageUrl + "?conversationId=" + m.ConversationId.ToString()));
        var count = await query.CountAsync(x => x.ReadAtUtc == null, ct);
        var rows = await query.OrderByDescending(x => x.ReadAtUtc == null).ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.StaffNotificationId).Take(30).ToListAsync(ct);
        // Negative IDs distinguish non-request activity when marking a combined inbox item read.
        return new(count, rows.Select(x => new RequestNotificationItem(-x.StaffNotificationId, x.Kind == "LeaveBalance" ? "Leave balance" : x.Kind,
            "", "", x.CreatedAtUtc, x.ReadAtUtc.HasValue, x.Url, Message: x.Message)).ToList());
    }

    private static async Task MarkStaffReadAsync(HrmTeContext db, Actor actor, long? notificationId, CancellationToken ct)
    {
        if(db.Model.FindEntityType(typeof(StaffNotification)) == null || notificationId is >= 0) return;
        var rows = await db.StaffNotifications.Where(x => x.UserId == actor.UserId && x.IndividualId == actor.PersonId && x.OrganisationId == actor.OrganisationId &&
            x.ReadAtUtc == null && (!notificationId.HasValue || x.StaffNotificationId == -notificationId.Value)).ToListAsync(ct);
        foreach(var row in rows) row.ReadAtUtc = DateTime.UtcNow;
    }

    private static async Task EnsureDutyAcceptanceNotificationsAsync(HrmTeContext db, Actor actor, CancellationToken ct)
    {
        if(db.Model.FindEntityType(typeof(DutyChangeRequest)) == null) return;
        var now = DateTime.UtcNow.AddHours(5);
        var pending = await db.DutyChangeRequests.AsNoTracking().Where(x => x.OrganisationId == actor.OrganisationId && x.IsOpen && x.Request.RequestStateId == 10 &&
            x.SourceStart > now && (!x.SwapStart.HasValue || x.SwapStart > now) &&
            ((x.SourceIndividualId == actor.PersonId && x.SourceAcceptedAtUtc == null) || (x.TargetIndividualId == actor.PersonId && x.AcceptedAtUtc == null)) &&
            !db.RequestNotifications.Any(n => n.UserId == actor.UserId && n.RequestId == x.RequestId && n.NotificationKind == "DutyAcceptance"))
            .Select(x => new { x.RequestId, x.Request.ReferenceNumber, x.CreatedAtUtc }).ToListAsync(ct);
        foreach(var row in pending) db.RequestNotifications.Add(new RequestNotification { RequestId = row.RequestId, UserId = actor.UserId,
            NotificationKind = "DutyAcceptance", FromStateId = 10, ToStateId = 10, ReferenceNumber = row.ReferenceNumber ?? $"DUTY-{row.RequestId}", CreatedAtUtc = row.CreatedAtUtc });
        if(pending.Count > 0)
        {
            try { await db.SaveChangesAsync(ct); }
            catch(DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                foreach(var entry in db.ChangeTracker.Entries<RequestNotification>().Where(x => x.State == EntityState.Added && x.Entity.NotificationKind == "DutyAcceptance").ToList()) entry.State = EntityState.Detached;
            }
        }
    }
}
