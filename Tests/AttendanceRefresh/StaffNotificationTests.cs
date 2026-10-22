using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services;
using Microsoft.EntityFrameworkCore;

static class StaffNotificationTests
{
    static void Check(bool value, string message) { if(!value) throw new Exception(message); }
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options);
        await using(var db = factory.CreateDbContext())
        {
            db.UserOrganisations.AddRange(new() { UserOrganisationID = 1, BusinessEntityID = 10 }, new() { UserOrganisationID = 2, BusinessEntityID = 20 });
            db.Users.AddRange(new() { UserId = 1, Username = "notify1", BusinessEntityID = 100, UserOrganisationId = 1, IsActive = true, LocalUserStateId = 1 },
                new() { UserId = 2, Username = "notify2", BusinessEntityID = 200, UserOrganisationId = 1, IsActive = true, LocalUserStateId = 1 },
                new() { UserId = 3, Username = "foreign", BusinessEntityID = 100, UserOrganisationId = 2, IsActive = true, LocalUserStateId = 1 },
                new() { UserId = 4, Username = "disabled", BusinessEntityID = 100, UserOrganisationId = 1, IsActive = false, LocalUserStateId = 1 });
            db.Jobs.AddRange(new() { JobId = 1, IndividualID = 100, OrganisationID = 10 }, new() { JobId = 2, IndividualID = 200, OrganisationID = 10 });
            db.Teams.Add(new() { TeamId = 1, OrganisationId = 10, Name = "Operations" });
            await db.SaveChangesAsync();
        }
        await using(var db = factory.CreateDbContext())
        {
            db.JobLeaveTypes.Add(new() { JobId = 1, IsValid = true, RemainingDays = 17.5m });
            db.JobWorkTemplateAssignments.Add(new() { JobID = 1, WorkTemplateID = 1, IsActive = true, SchedulePurpose = "Duty", EffectiveFrom = DateTime.Today });
            db.TeamStaffs.Add(new() { TeamId = 1, StaffId = 100, IsValid = true, StartDate = DateTime.Today });
            db.JobPositions.Add(new() { JobId = 1, PositionId = 1, FromDate = DateTime.Today });
            await db.SaveChangesAsync();
            var notifications = await db.StaffNotifications.ToListAsync();
            Check(notifications.Count == 4 && notifications.All(x => x.UserId == 1 && x.OrganisationId == 10), "Staff activity was missing, broadcast, or sent across tenants");
            Check(notifications.Any(x => x.Kind == "LeaveBalance" && x.Message.Contains("17.5")) && notifications.Any(x => x.Kind == "Schedule") && notifications.Any(x => x.Kind == "Team") && notifications.Any(x => x.Kind == "Position"), "Business actions lack meaningful notifications");
            (await db.JobLeaveTypes.SingleAsync()).RemainingDays = 20m; await db.SaveChangesAsync();
            Check(await db.StaffNotifications.CountAsync(x => x.Kind == "LeaveBalance") == 2, "Balance update did not produce an alert");
            await db.SaveChangesAsync(); Check(await db.StaffNotifications.CountAsync() == 5, "Unchanged save duplicated activity alerts");
            var schedule = await db.JobWorkTemplateAssignments.SingleAsync(); schedule.IsActive = false; await db.SaveChangesAsync();
            Check(await db.StaffNotifications.AnyAsync(x => x.Kind == "Schedule" && x.Message.Contains("ended")), "Schedule removal alert missing");
            (await db.Jobs.FindAsync(1))!.TerminatedDate = DateTime.Today; await db.SaveChangesAsync();
            Check(await db.StaffNotifications.AnyAsync(x => x.Kind == "Employment"), "Employment status alert missing");
        }
        var auth = new ContextAuthentication(); auth.SignIn("notify1"); var inboxService = new RequestNotificationService(factory, auth);
        var inbox = await inboxService.GetInboxAsync(); Check(inbox.UnreadCount == 7 && inbox.Items.All(x => x.Id < 0), "Bell did not combine staff action notifications");
        var first = inbox.Items.First(); await inboxService.MarkReadAsync(first.Id);
        Check((await inboxService.GetInboxAsync()).UnreadCount == 6, "Single staff activity could not be marked read");
        auth.SignIn("notify2"); Check((await inboxService.GetInboxAsync()).UnreadCount == 0, "Another employee can read staff activity");
        await inboxService.MarkReadAsync(first.Id);
        auth.SignIn("foreign"); Check((await inboxService.GetInboxAsync()).UnreadCount == 0, "Same individual in another tenant can read activity");
        auth.SignIn("notify1"); await inboxService.MarkReadAsync(); Check((await inboxService.GetInboxAsync()).UnreadCount == 0, "Mark all does not cover staff activity");
        await using(var db = factory.CreateDbContext()) Check(await db.StaffNotifications.CountAsync() == 7, "Reading the inbox generated extra business notifications");
        await using(var db = factory.CreateDbContext())
        {
            db.AttendanceLogs.Add(new AttendanceLog { AttendanceLogId = 1, IndividualId = 100, OrganisationId = 10, AttendanceLogStateId = 1, Date = DateTime.UtcNow.AddHours(5), InOutModeId = 1 });
            db.RequestTypes.Add(new HRM.Models.Archives.RequestType { RequestTypeId = 1, TypeName = "Attendance" });
            db.RequestStates.AddRange(new HRM.Models.Archives.RequestState { RequestStateId = 2, StateName = "Pending", IsProcessingState = true }, new HRM.Models.Archives.RequestState { RequestStateId = 4, StateName = "Approved", IsFinalState = true });
            db.Requests.Add(new Request { RequestId = 1, RequestTypeId = 1, RequestStateId = 2, ApplicantBusinessEntityId = 200, OrganisationBusinessEntityID = 10 });
            await db.SaveChangesAsync();
            db.AttendanceLogRequests.Add(new AttendanceLogRequest { AttendanceLogId = 1, RequestId = 1, Comments = "Submitted on behalf of staff" });
            await db.SaveChangesAsync();
            (await db.Requests.FindAsync(1))!.RequestStateId = 4; await db.SaveChangesAsync();
        }
        inbox = await inboxService.GetInboxAsync();
        Check(inbox.Items.Any(x => x.Reference == "Clock event") && inbox.Items.Any(x => x.Reference == "AttendanceRequest") && inbox.Items.Any(x => x.ToState == "Approved"), "Clock events or requests submitted on behalf of staff were not notified");
        var scope = Proxy.For<HRM.Services.Interfaces.IUserAccessService>((method, _) => method switch {
            "RequireContextAsync" => Task.FromResult(new HRM.DTOs.UserContext.UserContextDto { UserId = 1, IndividualId = 100 }),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(20), "HasOrganisationAccessAsync" => Task.FromResult(true), _ => throw new Exception(method) });
        Check((await new RequestNotificationService(factory, auth, scope).GetInboxAsync()).Items.Count == 0, "Bell ignored the selected tenant and used the account's home tenant");
    }
}
