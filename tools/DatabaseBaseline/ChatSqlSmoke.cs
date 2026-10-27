using System.Reflection;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

internal static class ChatSqlSmoke
{
    public static async Task SeedLegacyAsync(HrmTeContext db)
    {
        if(!db.Database.GetDbConnection().Database.StartsWith("HRM_LeaveUpgrade_",StringComparison.Ordinal))throw new InvalidOperationException("Legacy fixture is restricted to an isolated test database.");
        var users=await db.Users.AsNoTracking().Where(u=>u.UserOrganisation!=null).OrderBy(u=>u.UserId)
            .Select(u=>new{u.UserId,Org=u.UserOrganisation!.BusinessEntityID}).ToListAsync();
        var pair=users.GroupBy(u=>u.Org).First(g=>g.Count()>1).Take(2).OrderBy(u=>u.UserId).ToArray();
        await db.Database.OpenConnectionAsync();
        await using var command=db.Database.GetDbConnection().CreateCommand();
        void Add(string name,object value){var parameter=command.CreateParameter();parameter.ParameterName=name;parameter.Value=value;command.Parameters.Add(parameter);}
        Add("@org",pair[0].Org);Add("@first",pair[0].UserId);Add("@second",pair[1].UserId);Add("@key",Guid.NewGuid());
        command.CommandText="""
            INSERT dbo.CH_Conversations(OrganisationId,FirstUserId,SecondUserId,FirstReadMessageId,SecondReadMessageId,CreatedAtUtc,UpdatedAtUtc)
            VALUES(@org,@first,@second,0,0,DATEADD(hour,-1,SYSUTCDATETIME()),DATEADD(hour,-1,SYSUTCDATETIME()));
            DECLARE @chat bigint=SCOPE_IDENTITY();
            INSERT dbo.CH_Messages(ConversationId,SenderUserId,ClientMessageId,Body,RequestId,SentAtUtc)
            VALUES(@chat,@first,@key,N'Legacy history preservation fixture',NULL,DATEADD(hour,-1,SYSUTCDATETIME()));
            DECLARE @message bigint=SCOPE_IDENTITY();
            UPDATE dbo.CH_Conversations SET FirstReadMessageId=@message WHERE Id=@chat;
            """;
        await command.ExecuteNonQueryAsync();
    }
    public static async Task RunAsync(HrmTeContext source, bool groups=false)
    {
        if(!source.Database.GetDbConnection().Database.StartsWith("HRM_LeaveUpgrade_",StringComparison.Ordinal))
            throw new InvalidOperationException("Chat smoke writes are allowed only in the isolated restored test database.");
        var today=DateTime.UtcNow.AddHours(5).Date;
        var users=await source.Users.AsNoTracking().Where(u=>u.IsActive&&u.LocalUserStateId==1&&u.UserOrganisation!=null&&
            source.Jobs.Any(j=>j.IndividualID==u.BusinessEntityID&&j.OrganisationID==u.UserOrganisation.BusinessEntityID&&j.JobStateId==4&&j.JoinedDate<=today&&
                (!j.TerminatedDate.HasValue||j.TerminatedDate>today)&&source.JobPositions.Any(p=>p.JobId==j.JobId&&p.JobPositionStateId==4&&p.FromDate<=today&&(!p.ToDate.HasValue||p.ToDate>today))))
            .Select(u=>new{u.UserId,Person=u.BusinessEntityID,Org=u.UserOrganisation!.BusinessEntityID}).ToListAsync();
        var pair=users.GroupBy(u=>u.Org).Where(g=>g.Count()>(groups?2:1)).OrderByDescending(g=>g.Count()).First().Take(groups?3:2).ToArray();
        var actor=pair[0];
        var access=DispatchProxy.Create<IUserAccessService,ChatSmokeAccess>();
        ((ChatSmokeAccess)(object)access).Handler=(m,a)=>m switch{
            "RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=actor.UserId,IndividualId=actor.Person}),
            "GetCurrentOrganisationIdAsync"=>Task.FromResult(actor.Org),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! == actor.Org),_=>throw new InvalidOperationException(m)};
        var factory=new ChatSmokeFactory(source.Database.GetConnectionString()!);
        var service=new StaffChatService(factory,access);
        await service.SearchContactsAsync("");await service.SearchContactsAsync("","hr");await service.SearchContactsAsync("","supervisor");
        var id=await service.StartAsync(pair[1].UserId);var key=Guid.NewGuid();
        var sent=await service.SendAsync(id,"Isolated SQL integration test",null,key);
        if(await service.SendAsync(id,"Isolated SQL integration test",null,key)!=sent)throw new InvalidOperationException("SQL retry duplicated message.");
        await service.SearchRequestsAsync(id,"");
        actor=pair[1];
        if(!(await service.ListAsync()).Any(c=>c.Id==id&&c.Unread>0))throw new InvalidOperationException("SQL unread count missing.");
        var history=await service.ReadAsync(id);
        if(!history.Messages.Any(m=>m.Id==sent)||!(await service.ListAsync()).Any(c=>c.Id==id&&c.Unread==0))throw new InvalidOperationException("SQL history/read tracking failed.");
        if(groups)
        {
            actor=pair[0];
            var group=await service.CreateGroupAsync("Isolated group test",[pair[1].UserId]);
            var first=await service.SendAsync(group,"Before new member",null,Guid.NewGuid());
            await service.AddMemberAsync(group,pair[2].UserId);
            actor=pair[2];if((await service.ReadAsync(group)).Messages.Count!=0)throw new InvalidOperationException("New SQL member saw old messages.");
            actor=pair[0];var current=await service.SendAsync(group,"For current members",null,Guid.NewGuid());
            await service.RemoveMemberAsync(group,pair[1].UserId);
            actor=pair[1];var denied=false;try{await service.ReadAsync(group);}catch(UnauthorizedAccessException){denied=true;}
            if(!denied)throw new InvalidOperationException("Removed SQL member could read.");
            var username=await source.Users.Where(u=>u.UserId==actor.UserId).Select(u=>u.Username).SingleAsync();
            var auth=new ChatSmokeAuthentication(username);
            var bell=await new RequestNotificationService(factory,auth,access).GetInboxAsync();
            if(bell.Items.Any(i=>i.Url==$"/online/chat?conversationId={group}"))throw new InvalidOperationException("Removed member retained SQL chat notification links.");
            Console.WriteLine("PASS SQL chat notifications: removed membership hides old message/invitation links.");
            actor=pair[0];var gap=await service.SendAsync(group,"During absence",null,Guid.NewGuid());
            await service.AddMemberAsync(group,pair[1].UserId);
            actor=pair[1];var rejoined=await service.ReadAsync(group);
            if(!rejoined.Messages.Any(m=>m.Id==first)||!rejoined.Messages.Any(m=>m.Id==current)||rejoined.Messages.Any(m=>m.Id==gap))throw new InvalidOperationException("SQL rejoin history bounds failed.");
            actor=pair[0];await service.ConvertToGroupAsync(id,"Converted SQL conversation");await service.AddMemberAsync(id,pair[2].UserId);
            actor=pair[2];if((await service.ReadAsync(id)).Messages.Count!=0)throw new InvalidOperationException("SQL conversion leaked direct history.");
            actor=pair[0];var newDirect=await service.StartAsync(pair[1].UserId);
            if(newDirect==id||(await service.ReadAsync(newDirect)).IsGroup)throw new InvalidOperationException("SQL filtered direct-pair index prevented a new private chat.");
            Console.WriteLine("PASS isolated SQL group chat: membership management, removal/rejoin history, visibility bounds and direct conversion.");
        }
        // Exercise the protected view with an existing request in this isolated copy.
        var people=users.Select(u=>u.Person).ToArray();
        var request=await source.Requests.AsNoTracking().Where(r=>!r.RequestState.IsFinalState&&r.ApplicantBusinessEntityId.HasValue&&people.Contains(r.ApplicantBusinessEntityId.Value))
            .OrderByDescending(r=>r.RequestId).FirstOrDefaultAsync();
        if(request!=null)
        {
            var owner=users.First(u=>u.Person==request.ApplicantBusinessEntityId&&u.Org==request.OrganisationBusinessEntityID);
            var job=await source.Jobs.Where(j=>j.IndividualID==owner.Person&&j.OrganisationID==owner.Org&&j.JobStateId==4&&j.JoinedDate<=today&&(!j.TerminatedDate.HasValue||j.TerminatedDate>today))
                .OrderByDescending(j=>j.JoinedDate).Select(j=>j.JobId).FirstAsync();
            var supervisors=await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(source,job);
            var unrelated=users.FirstOrDefault(u=>u.Org==owner.Org&&u.UserId!=owner.UserId&&!supervisors.Contains(u.Person));
            if(unrelated!=null)
            {
                actor=owner;var privacy=await service.StartAsync(unrelated.UserId);
                var infoId=await service.SendAsync(privacy,"Isolated request information-only test",request.RequestId,Guid.NewGuid());
                if((await service.GetRequestDetailsAsync(privacy,request.RequestId)).Reference!=(request.ReferenceNumber??$"#{request.RequestId}"))throw new InvalidOperationException("Requester could not open SQL protected view.");
                actor=unrelated;var info=(await service.ReadAsync(privacy)).Messages.Single(m=>m.Id==infoId).Request;
                if(info==null||info.Url!=null)throw new InvalidOperationException("Information-only SQL recipient was hidden or received a detail link.");
                var denied=false;try{await service.GetRequestDetailsAsync(privacy,request.RequestId);}catch(UnauthorizedAccessException){denied=true;}
                if(!denied)throw new InvalidOperationException("Unrelated SQL participant opened protected request details.");
                Console.WriteLine("PASS SQL request privacy: requester detail view, shared information-only reference and direct URL access denial.");
            }
        }
        Console.WriteLine("PASS isolated SQL chat: directory filters, conversation, send/retry, request search, unread counts and read tracking.");
    }
    sealed class ChatSmokeFactory(string connection):IDbContextFactory<HrmTeContext>
    {public HrmTeContext CreateDbContext()=>new(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);}
}
public class ChatSmokeAccess:DispatchProxy
{
    public Func<string,object?[],object?> Handler {get;set;}=null!;
    protected override object? Invoke(MethodInfo? method,object?[]? args)=>Handler(method!.Name,args??[]);
}

internal sealed class ChatSmokeAuthentication(string username):Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
{
    public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(
        new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("idnumber",username)],"IsolatedSqlTest"))));
}
