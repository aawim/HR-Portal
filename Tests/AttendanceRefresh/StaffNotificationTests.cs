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
            db.JobWorkTemplateAssignments.Add(new() { JobID = 1, WorkTemplateID = 1, IsActive = true, SchedulePurpose = "Duty", EffectiveFrom = DateTime.UtcNow.AddHours(5).Date });
            db.TeamStaffs.Add(new() { TeamId = 1, StaffId = 100, IsValid = true, StartDate = DateTime.UtcNow.AddHours(5).Date });
            db.JobPositions.Add(new() { JobId = 1, PositionId = 1, FromDate = DateTime.UtcNow.AddHours(5).Date });
            await db.SaveChangesAsync();
            var notifications = await db.StaffNotifications.ToListAsync();
            Check(notifications.Count == 4 && notifications.All(x => x.UserId == 1 && x.OrganisationId == 10), "Staff activity was missing, broadcast, or sent across tenants");
            Check(notifications.Any(x => x.Kind == "LeaveBalance" && x.Message.Contains("17.5")) && notifications.Any(x => x.Kind == "Schedule") && notifications.Any(x => x.Kind == "Team") && notifications.Any(x => x.Kind == "Position"), "Business actions lack meaningful notifications");
            (await db.JobLeaveTypes.SingleAsync()).RemainingDays = 20m; await db.SaveChangesAsync();
            Check(await db.StaffNotifications.CountAsync(x => x.Kind == "LeaveBalance") == 2, "Balance update did not produce an alert");
            await db.SaveChangesAsync(); Check(await db.StaffNotifications.CountAsync() == 5, "Unchanged save duplicated activity alerts");
            var schedule = await db.JobWorkTemplateAssignments.SingleAsync(); schedule.IsActive = false; await db.SaveChangesAsync();
            Check(await db.StaffNotifications.AnyAsync(x => x.Kind == "Schedule" && x.Message.Contains("ended")), "Schedule removal alert missing");
            // Keep this fixture active for later clock/request notification checks; former-staff writes have their own denial tests.
            (await db.Jobs.FindAsync(1))!.TerminatedDate = DateTime.UtcNow.AddHours(5).Date.AddDays(1); await db.SaveChangesAsync();
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
            var notificationCount = await db.StaffNotifications.CountAsync();
            var quietClock = new AttendanceLog { AttendanceLogId = 2, IndividualId = 100, OrganisationId = 10, AttendanceLogStateId = 1, Date = DateTime.UtcNow.AddHours(5), InOutModeId = 1 };
            db.AttendanceLogs.Add(quietClock); await db.SaveChangesAsync();
            quietClock.Date = quietClock.Date.AddMinutes(1); quietClock.InOutModeId = 2; db.SaveChanges();
            db.AttendanceLogs.Remove(quietClock); await db.SaveChangesAsync();
            Check(await db.StaffNotifications.CountAsync() == notificationCount, "Creating, editing or deleting a clock event sent a notification");
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
        Check(!inbox.Items.Any(x => x.Reference == "Clock event") && inbox.Items.Any(x => x.Reference == "AttendanceRequest") && inbox.Items.Any(x => x.ToState == "Approved"), "Clock events were notified or attendance request notifications were lost");
        var scope = Proxy.For<HRM.Services.Interfaces.IUserAccessService>((method, _) => method switch {
            "RequireContextAsync" => Task.FromResult(new HRM.DTOs.UserContext.UserContextDto { UserId = 1, IndividualId = 100 }),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(20), "HasOrganisationAccessAsync" => Task.FromResult(true), _ => throw new Exception(method) });
        Check((await new RequestNotificationService(factory, auth, scope).GetInboxAsync()).Items.Count == 0, "Bell ignored the selected tenant and used the account's home tenant");
        await using(var db=factory.CreateDbContext())
        {
            db.Users.Add(new(){UserId=5,Username="other-member",BusinessEntityID=500,UserOrganisationId=1,IsActive=true,LocalUserStateId=1});
            db.StaffChatConversations.AddRange(new StaffChatConversation{Id=10,OrganisationId=10,IsGroup=true,Title="Member chat",FirstUserId=1,SecondUserId=2},
                new StaffChatConversation{Id=11,OrganisationId=10,IsGroup=true,Title="Other chat",FirstUserId=2,SecondUserId=5},
                new StaffChatConversation{Id=12,OrganisationId=10,IsGroup=true,Title="Former chat",FirstUserId=1,SecondUserId=2});
            db.StaffChatMemberships.AddRange(new StaffChatMembership{ConversationId=10,UserId=1},new StaffChatMembership{ConversationId=10,UserId=2},
                new StaffChatMembership{ConversationId=11,UserId=2},new StaffChatMembership{ConversationId=11,UserId=5},
                new StaffChatMembership{ConversationId=12,UserId=1,LeftAtUtc=DateTime.UtcNow},new StaffChatMembership{ConversationId=12,UserId=2});
            foreach(var id in new[]{10,11,12})db.StaffNotifications.Add(new(){UserId=1,IndividualId=100,OrganisationId=10,Kind="Chat",Message="Chat activity",Url=$"/online/chat?conversationId={id}",CreatedAtUtc=DateTime.UtcNow,ReadAtUtc=id==12?DateTime.UtcNow:null});
            db.StaffNotifications.Add(new(){UserId=1,IndividualId=100,OrganisationId=10,Kind="ChatMembership",Message="Old group invitation",Url="/online/chat?conversationId=12",CreatedAtUtc=DateTime.UtcNow});
            db.StaffNotifications.Add(new(){UserId=1,IndividualId=100,OrganisationId=10,Kind="ChatMembership",Message="You were removed from a group.",Url="/online/chat",CreatedAtUtc=DateTime.UtcNow});
            await db.SaveChangesAsync();
        }
        inbox=await inboxService.GetInboxAsync();
        Check(inbox.Items.Any(i=>i.Url=="/online/chat?conversationId=10")&&!inbox.Items.Any(i=>i.Url is "/online/chat?conversationId=11" or "/online/chat?conversationId=12"),"Non-member or removed chat links appeared in the bell");
        Check(inbox.Items.Any(i=>i.Message=="You were removed from a group."),"Personal removal notice was lost");
        await inboxService.MarkReadAsync();
        await using(var db=factory.CreateDbContext())
        {
            var now=DateTime.UtcNow;
            for(var i=0;i<40;i++)
            {
                db.StaffNotifications.Add(new(){UserId=1,IndividualId=100,OrganisationId=10,Kind="History",Message="Newer read staff alert",Url="/online",CreatedAtUtc=now.AddMinutes(-i),ReadAtUtc=now});
                db.RequestNotifications.Add(new(){UserId=1,RequestId=1,NotificationKind="StateChanged",ReferenceNumber="Newer read request alert",FromStateId=2,ToStateId=4,CreatedAtUtc=now.AddMinutes(-i),ReadAtUtc=now});
            }
            db.StaffNotifications.Add(new(){UserId=1,IndividualId=100,OrganisationId=10,Kind="History",Message="Older unread staff alert",Url="/online",CreatedAtUtc=now.AddDays(-2)});
            db.RequestNotifications.Add(new(){UserId=1,RequestId=1,NotificationKind="StateChanged",ReferenceNumber="Older unread request alert",FromStateId=2,ToStateId=4,CreatedAtUtc=now.AddDays(-3)});
            await db.SaveChangesAsync();
        }
        inbox=await inboxService.GetInboxAsync();
        Check(inbox.UnreadCount==2&&inbox.Items.Count==30,"Unread prioritisation changed counts or inbox limit");
        Check(inbox.Items[0].Message=="Older unread staff alert"&&inbox.Items[1].Reference=="Older unread request alert"&&inbox.Items.Skip(2).All(i=>i.IsRead),"Older unread alerts were buried by newer read items or truncated before merging");
        Check(inbox.Items.Skip(2).Select(i=>i.CreatedAtUtc).SequenceEqual(inbox.Items.Skip(2).OrderByDescending(i=>i.CreatedAtUtc).Select(i=>i.CreatedAtUtc)),"Read notifications lost newest-first ordering");
        await inboxService.MarkReadAsync(inbox.Items[0].Id);
        inbox=await inboxService.GetInboxAsync();
        Check(inbox.UnreadCount==1&&inbox.Items[0].Reference=="Older unread request alert"&&!inbox.Items[0].IsRead&&inbox.Items.Skip(1).All(i=>i.IsRead),"Marking read did not move the item below remaining unread alerts");


    }
}
