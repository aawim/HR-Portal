using HRM.Helpers;
using HRM.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record RequestNotificationItem(long Id, string Reference, string FromState, string ToState,
    DateTime CreatedAtUtc, bool IsRead, string Url);
public sealed record RequestNotificationInbox(int UnreadCount, List<RequestNotificationItem> Items);

public sealed class RequestNotificationService(IDbContextFactory<HrmTeContext> factory, AuthenticationStateProvider authentication)
{
    private async Task<IQueryable<RequestNotification>> OwnedAsync(HrmTeContext db, CancellationToken token)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var username = principal.FindFirst("idnumber")?.Value;
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(username))
            return db.RequestNotifications.Where(x => false);
        var user = await db.Users.AsNoTracking().Where(x => x.Username == username && x.IsActive && x.LocalUserStateId == 1)
            .Select(x => new { x.UserId, OrganisationId = (int?)x.UserOrganisation!.BusinessEntityID }).SingleOrDefaultAsync(token);
        if (user == null) return db.RequestNotifications.Where(x => false);
        return db.RequestNotifications.Where(x => x.UserId == user.UserId &&
            (x.Request.OrganisationBusinessEntityID == null || x.Request.OrganisationBusinessEntityID == user.OrganisationId));
    }

    public async Task<RequestNotificationInbox> GetInboxAsync(CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var owned = await OwnedAsync(db, token);
        var unread = await owned.CountAsync(x => x.ReadAtUtc == null, token);
        var rows = await (from n in owned.AsNoTracking()
                          join fromState in db.RequestStates on n.FromStateId equals fromState.RequestStateId
                          join toState in db.RequestStates on n.ToStateId equals toState.RequestStateId
                          orderby n.CreatedAtUtc descending, n.RequestNotificationId descending
                          select new { n.RequestNotificationId, n.ReferenceNumber, n.CreatedAtUtc, n.ReadAtUtc,
                              FromState = fromState.StateName, ToState = toState.StateName, n.Request.RequestTypeId,
                              IsLeave = db.LeaveRequests.Any(x => x.RequestId == n.RequestId),
                              IsAttendance = n.Request.AttendanceLogChangeRequest != null || n.Request.AttendanceLogRequests.Any() })
                          .Take(30).ToListAsync(token);
        return new(unread, rows.Select(n => new RequestNotificationItem(n.RequestNotificationId, n.ReferenceNumber,
            n.FromState.Trim(), n.ToState.Trim(), n.CreatedAtUtc, n.ReadAtUtc.HasValue,
            LeaveChangeRequestTypes.IsChange(n.RequestTypeId) ? "/online/leaves/leave-change-request" :
            n.IsLeave ? "/online/leaves/all-requests" : n.IsAttendance ? "/online/attendance/attendance-request" : "/online")).ToList());
    }

    public async Task MarkReadAsync(long? notificationId = null, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var owned = await OwnedAsync(db, token);
        var items = await owned.Where(x => x.ReadAtUtc == null && (!notificationId.HasValue || x.RequestNotificationId == notificationId.Value)).ToListAsync(token);
        foreach (var item in items) item.ReadAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
    }
}
