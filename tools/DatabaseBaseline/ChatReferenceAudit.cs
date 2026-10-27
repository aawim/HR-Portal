using HRM.Models;
using Microsoft.EntityFrameworkCore;
internal static class ChatReferenceAudit
{
    public static async Task RunAsync(HrmTeContext db)
    {
        var rows=await db.Requests.AsNoTracking().Where(r=>r.ApplicantBusinessEntityId==811||db.LeaveRequests.Any(l=>l.RequestId==r.RequestId&&l.Leave.Job.IndividualID==811))
            .OrderByDescending(r=>r.RequestId).Take(15).Select(r=>new{r.RequestId,r.ReferenceNumber,r.OrganisationBusinessEntityID,r.ApplicantBusinessEntityId,Type=r.RequestType.TypeName,Status=r.RequestState.StateName,r.RequestState.IsFinalState}).ToListAsync();
        foreach(var row in rows)Console.WriteLine($"REQUEST {row.RequestId}: {row.ReferenceNumber}, org={row.OrganisationBusinessEntityID}, applicant={row.ApplicantBusinessEntityId}, {row.Type}, {row.Status}, final={row.IsFinalState}");
        foreach(var chat in await db.StaffChatConversations.AsNoTracking().OrderByDescending(c=>c.Id).Take(10).ToListAsync())
        {
            var members=await db.StaffChatMemberships.AsNoTracking().Where(m=>m.ConversationId==chat.Id&&m.LeftAtUtc==null).Join(db.Users,m=>m.UserId,u=>u.UserId,(m,u)=>new{u.UserId,u.FullName}).ToListAsync();
            Console.WriteLine($"CHAT {chat.Id}, org={chat.OrganisationId}, group={chat.IsGroup}, members="+string.Join("; ",members.Select(m=>$"{m.UserId}:{m.FullName}")));
            if(chat.Id==2)
                foreach(var member in members)
                {
                    var user=await db.Users.AsNoTracking().SingleAsync(u=>u.UserId==member.UserId);
                    var access=System.Reflection.DispatchProxy.Create<HRM.Services.Interfaces.IUserAccessService,ChatSmokeAccess>();
                    ((ChatSmokeAccess)(object)access).Handler=(method,args)=>method switch{
                        "RequireContextAsync"=>Task.FromResult(new HRM.DTOs.UserContext.UserContextDto{UserId=user.UserId,IndividualId=user.BusinessEntityID}),
                        "GetCurrentOrganisationIdAsync"=>Task.FromResult(chat.OrganisationId),"HasOrganisationAccessAsync"=>Task.FromResult((int)args[0]! ==chat.OrganisationId),_=>throw new InvalidOperationException(method)};
                    var factory=new AuditFactory(db.Database.GetConnectionString()!);
                    var chatService=new HRM.Services.StaffChatService(factory,access);
                    foreach(var term in new[]{"","Annual","Rasheeda","ALR/2026/2780"})
                        Console.WriteLine($"ATTACHABLE for user {user.UserId}, search '{term}': "+string.Join(",",(await chatService.SearchRequestsAsync(chat.Id,term)).Select(r=>r.Reference+(r.Url==null?" [information only]":" [private link]"))));
                    var pending=await db.Requests.AsNoTracking().SingleAsync(r=>r.RequestId==23683);
                    var localDay=pending.ApplicationDate.AddHours(5).Date;
                    var filtered=await chatService.SearchRequestsAsync(chat.Id,pending.ReferenceNumber,localDay,localDay,pending.RequestStateId);
                    Console.WriteLine($"FILTERED for user {user.UserId}, local submitted day {localDay:yyyy-MM-dd}, state {pending.RequestStateId}: "+string.Join(",",filtered.Select(r=>r.Reference)));
                    var keys=await new HRM.Services.EffectivePermissionService(factory).GetEffectivePermissionsAsync(user.UserId);
                    Console.WriteLine("BROAD "+string.Join(",",keys.Where(k=>k.RoleKey is "Admin" or "Requests.View" or "Super.Admin.Manage").Select(k=>k.RoleKey)));
                }
        }
    }
    sealed class AuditFactory(string connection):IDbContextFactory<HrmTeContext>{public HrmTeContext CreateDbContext()=>new(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);}
}
