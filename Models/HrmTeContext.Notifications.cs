using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CaptureStateNotificationsAsync(false, CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await CaptureStateNotificationsAsync(true, cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task CaptureStateNotificationsAsync(bool asynchronous, CancellationToken token)
    {
        await CaptureStaffNotificationsAsync(asynchronous, token);
        if (Model.FindEntityType(typeof(RequestNotification)) == null) return;
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<Request>().Where(x => x.State == EntityState.Added).ToList())
        {
            var request = entry.Entity;
            if (!request.ApplicantBusinessEntityId.HasValue) continue;
            var users = Users.Where(u => u.IsActive && u.LocalUserStateId == 1 &&
                u.BusinessEntityID == request.ApplicantBusinessEntityId &&
                (request.OrganisationBusinessEntityID == null || u.UserOrganisation!.BusinessEntityID == request.OrganisationBusinessEntityID))
                .Select(u => u.UserId);
            var ids = asynchronous ? await users.ToListAsync(token).ConfigureAwait(false) : users.ToList();
            foreach (var id in ids)
            {
                if (ChangeTracker.Entries<RequestNotification>().Any(n => n.State == EntityState.Added &&
                    ReferenceEquals(n.Entity.Request, request) && n.Entity.UserId == id && n.Entity.NotificationKind == "RequestSubmitted")) continue;
                RequestNotifications.Add(new RequestNotification { Request = request, UserId = id,
                    NotificationKind = "RequestSubmitted", FromStateId = request.RequestStateId, ToStateId = request.RequestStateId,
                    ReferenceNumber = request.ReferenceNumber ?? "New request", CreatedAtUtc = DateTime.UtcNow });
            }
        }
        var changes = ChangeTracker.Entries<Request>().Where(x => x.State == EntityState.Modified &&
            (x.Property(r => r.RequestStateId).OriginalValue != x.Property(r => r.RequestStateId).CurrentValue ||
             x.Property(r => r.LastStateChangedDate).IsModified || x.Property(r => r.StateChangeRemarks).IsModified)).ToList();
        foreach (var entry in changes)
        {
            var request = entry.Entity;
            var from = entry.Property(x => x.RequestStateId).OriginalValue;
            var people = new List<int>();
            if (request.ApplicantBusinessEntityId.HasValue) people.Add(request.ApplicantBusinessEntityId.Value);
            var leaveOwners = LeaveRequests.Where(x => x.RequestId == request.RequestId).Select(x => x.Leave.Job.IndividualID)
                .Union(LeaveChangeRequests.Where(x => x.RequestId == request.RequestId).Select(x => x.Leave.Job.IndividualID));
            people.AddRange(asynchronous ? await leaveOwners.ToListAsync(token).ConfigureAwait(false) : leaveOwners.ToList());
            if(Model.FindEntityType(typeof(HRM.Models.Archives.RequestJob)) != null)
            {
                var subjects = RequestJobs.Where(x => x.RequestId == request.RequestId && x.IsValid &&
                    (request.OrganisationBusinessEntityID == null || x.Job.OrganisationID == request.OrganisationBusinessEntityID)).Select(x => x.Job.IndividualID);
                people.AddRange(asynchronous ? await subjects.ToListAsync(token) : subjects.ToList());
            }
            if(Model.FindEntityType(typeof(AttendanceLogRequest)) != null)
            {
                var subjects = AttendanceLogRequests.Where(x => x.RequestId == request.RequestId &&
                    (request.OrganisationBusinessEntityID == null || x.AttendanceLog.OrganisationId == request.OrganisationBusinessEntityID)).Select(x => x.AttendanceLog.IndividualId);
                people.AddRange(asynchronous ? await subjects.ToListAsync(token) : subjects.ToList());
            }
            if(Model.FindEntityType(typeof(AttendanceLogChangeRequest)) != null)
            {
                var subjects = AttendanceLogChangeRequests.Where(x => x.RequestId == request.RequestId &&
                    (request.OrganisationBusinessEntityID == null || x.AttendanceLog.OrganisationId == request.OrganisationBusinessEntityID)).Select(x => x.AttendanceLog.IndividualId);
                people.AddRange(asynchronous ? await subjects.ToListAsync(token) : subjects.ToList());
            }
            var users = Users.Where(x => x.IsActive && x.LocalUserStateId == 1 && people.Contains(x.BusinessEntityID) &&
                (request.OrganisationBusinessEntityID == null || x.UserOrganisation!.BusinessEntityID == request.OrganisationBusinessEntityID))
                .Select(x => x.UserId);
            var recipients = asynchronous ? await users.ToListAsync(token).ConfigureAwait(false) : users.ToList();
            if (recipients.Count == 0 && request.ApplicantBusinessEntityId == null && people.Count == 0)
            {
                var creators = OperationLogs.Where(x => x.OperationLogId == request.OperationLogId && x.CreatedByUserId != null)
                    .Select(x => x.CreatedByUserId!.Value);
                var fallback = Users.Where(x => creators.Contains(x.UserId) && x.IsActive && x.LocalUserStateId == 1 &&
                    (request.OrganisationBusinessEntityID == null || x.UserOrganisation!.BusinessEntityID == request.OrganisationBusinessEntityID))
                    .Select(x => x.UserId);
                recipients = asynchronous ? await fallback.ToListAsync(token).ConfigureAwait(false) : fallback.ToList();
            }
            foreach (var userId in recipients.Distinct())
            {
                // A retry on the same context must not enqueue the same pending change twice.
                if (ChangeTracker.Entries<RequestNotification>().Any(x => x.State == EntityState.Added &&
                    x.Entity.RequestId == request.RequestId && x.Entity.UserId == userId &&
                    x.Entity.FromStateId == from && x.Entity.ToStateId == request.RequestStateId)) continue;
                RequestNotifications.Add(new RequestNotification { UserId = userId, RequestId = request.RequestId,
                    NotificationKind = from == request.RequestStateId ? "RequestAction" : "StateChanged",
                    FromStateId = from, ToStateId = request.RequestStateId,
                    ReferenceNumber = request.ReferenceNumber ?? $"#{request.RequestId}", CreatedAtUtc = DateTime.UtcNow });
            }
        }
        // The state change and its notifications are saved in the same transaction.
        // Rolled-back changes never become visible to another user's circuit.
    }
}
