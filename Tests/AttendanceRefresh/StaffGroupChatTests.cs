using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class StaffGroupChatTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Denied(Func<Task> action){try{await action();}catch(UnauthorizedAccessException){return;}throw new Exception("Expected group access denial");}
    static async Task Invalid(Func<Task> action){try{await action();}catch(InvalidOperationException){return;}throw new Exception("Expected group validation failure");}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),x=>x.EnableNullChecks(false))
            .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var actor=1;var org=10;
        void Act(int user){actor=user;org=user==5?20:10;}
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{
            "RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=actor,IndividualId=actor*100,IsSuperAdministrator=actor==4}),
            "GetCurrentOrganisationIdAsync"=>Task.FromResult(org),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! ==org),_=>throw new Exception(m)});
        var service=new StaffChatService(factory,access);var today=DateTime.UtcNow.AddHours(5).Date;
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation{BusinessEntityID=10,OrganisationName="Tenant"},new Organisation{BusinessEntityID=20,OrganisationName="Other"});
            db.UserOrganisations.AddRange(new UserOrganisation{UserOrganisationID=1,BusinessEntityID=10},new UserOrganisation{UserOrganisationID=2,BusinessEntityID=20});
            for(var id=1;id<=5;id++)
            {
                db.Users.Add(new(){UserId=id,BusinessEntityID=id*100,FullName="Staff "+id,Username="user"+id,IsActive=true,LocalUserStateId=1,UserOrganisationId=id==5?2:1,
                    AdminPortalAccessEnabled=id is 2 or 3,AdminPortalAccessOrganisationId=id is 2 or 3?1:null});
                db.Jobs.Add(new(){JobId=id,IndividualID=id*100,OrganisationID=id==5?20:10,JobStateId=4,OrganisationStructureId=id==5?0:1,JoinedDate=today.AddYears(-1)});
                db.JobPositions.Add(new(){JobPositionId=id,JobId=id,JobPositionStateId=4,FromDate=today.AddMonths(-1)});
            }
            db.OrganisationStructures.Add(new(){OrganisationStructureId=1,OrganisationBusinessEntityID=10,OrganisationStructureStateId=3,Name="Department"});
            db.OrganisationStructureSupervisors.Add(new(){OrganisationStructureId=1,SupervisorStaffId=300,IsActive=true,IsValid=true});
            db.Roles.Add(new(){RoleID=1,RoleKey="Requests.View",Name="View requests",ContextId=1});
            db.UserOrganisationRoles.Add(new(){UserOrganisationID=1,RoleID=1,IsActive=true});
            foreach(var user in new[]{2,3})db.UserRoles.Add(new(){UserId=user,UserOrganisationId=1,RoleId=1,IsActive=true});
            db.RequestStates.AddRange(new RequestState{RequestStateId=2,StateName="Pending Verification",IsFinalState=false,IsProcessingState=true},
                new RequestState{RequestStateId=4,StateName="Approved",IsFinalState=true},new RequestState{RequestStateId=5,StateName="Cancelled",IsFinalState=true},
                new RequestState{RequestStateId=6,StateName="Rejected",IsFinalState=true});
            db.RequestTypes.Add(new(){RequestTypeId=1,TypeName="Leave",ViewUrl="/online/leaves"});
            db.Requests.Add(new(){RequestId=1,RequestStateId=2,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=100,RequestTypeId=1,ReferenceNumber="LEAVE-1"});
            await db.SaveChangesAsync();
        }
        await Denied(()=>service.CreateGroupAsync("Foreign",[2,5]));
        await Invalid(()=>service.CreateGroupAsync("Empty",[]));
        var group=await service.CreateGroupAsync("Request discussion",[2]);
        var first=await service.SendAsync(group,"Original request",1,Guid.NewGuid());
        Act(2);var page=await service.ReadAsync(group);Check(page.IsGroup&&!page.CanManage&&page.Messages.Single().Request?.Id==1,"Group recipient or request failed");
        await Denied(()=>service.AddMemberAsync(group,3));await Denied(()=>service.RemoveMemberAsync(group,1));await Denied(()=>service.SetManagerAsync(group,2,true));
        await Denied(()=>service.RenameGroupAsync(group,"Hijack"));
        Act(1);await service.AddMemberAsync(group,3);await Invalid(()=>service.AddMemberAsync(group,3));await Denied(()=>service.AddMemberAsync(group,5));
        Act(3);Check((await service.ReadAsync(group)).Messages.Count==0,"New member saw pre-join history");
        await Denied(()=>service.GetRequestDetailsAsync(group,1));
        Check((await service.ListAsync()).Single().Unread==0,"New member inherited unread history");
        Act(1);var second=await service.SendAsync(group,"Shared current request",1,Guid.NewGuid());
        Act(3);page=await service.ReadAsync(group);Check(page.Messages.Select(m=>m.Id).SequenceEqual([second]),"New member visibility incorrect");
        Check((await service.GetRequestDetailsAsync(group,1)).Reference=="LEAVE-1","Current supervisor without workflow assignment could not open shared details");
        Act(2);var authored=await service.SendAsync(group,"I checked the request",null,Guid.NewGuid());
        Act(1);await service.RemoveMemberAsync(group,2);
        page=await service.ReadAsync(group);Check(page.Messages.Any(m=>m.Id==authored&&m.SenderName=="Staff 2"),"Removing author deleted or relabelled messages");
        Act(2);await Denied(()=>service.ReadAsync(group));await Denied(()=>service.SendAsync(group,"Removed",null,Guid.NewGuid()));await Denied(()=>service.SearchRequestsAsync(group,"LEAVE"));
        Check(!(await service.ListAsync()).Any(c=>c.Id==group),"Removed group still listed");
        int alerts;await using(var db=factory.CreateDbContext())alerts=await db.StaffNotifications.CountAsync(n=>n.UserId==2&&n.Kind=="Chat");
        Act(1);var gap=await service.SendAsync(group,"Message while removed",null,Guid.NewGuid());
        await using(var db=factory.CreateDbContext())Check(await db.StaffNotifications.CountAsync(n=>n.UserId==2&&n.Kind=="Chat")==alerts,"Removed member received a message alert");
        await service.AddMemberAsync(group,2);
        Act(2);page=await service.ReadAsync(group);
        Check(page.Messages.Any(m=>m.Id==first)&&page.Messages.Any(m=>m.Id==authored)&&!page.Messages.Any(m=>m.Id==gap),"Rejoining lost old history or exposed removal-gap history");
        Check(!page.CanManage,"Rejoining restored manager rights implicitly");
        Act(1);var after=await service.SendAsync(group,"After rejoining",null,Guid.NewGuid());
        Act(2);Check((await service.ReadAsync(group)).Messages.Any(m=>m.Id==after),"Rejoined member cannot read new messages");
        Act(1);await service.AddMemberAsync(group,4);
        Act(4);Check((await service.ReadAsync(group)).Messages.Count==0,"Administrator new member bypassed join history boundary");
        Act(1);await service.SendAsync(group,"Request information for the group",1,Guid.NewGuid());
        Check((await service.SearchRequestsAsync(group,"LEAVE")).Any(r=>r.Id==1),"Information-only members blocked an authorised sender's attachment");
        Act(4);var info=(await service.ReadAsync(group)).Messages.Single().Request;
        Check(info?.Id==1&&info.Url==null,"Unrelated group member did not receive an information-only reference");
        await Denied(()=>service.GetRequestDetailsAsync(group,1));
        Check(!(await service.SearchRequestsAsync(group,"LEAVE")).Any(),"Unrelated member could browse private request records");
        Act(1);await service.RemoveMemberAsync(group,4);
        Act(4);await Denied(()=>service.ReadAsync(group));await Denied(()=>service.AddMemberAsync(group,4));
        Act(1);await service.SetManagerAsync(group,3,true);
        Act(3);Check((await service.ReadAsync(group)).CanManage&&!(await service.ReadAsync(group)).Messages.Any(m=>m.Id==first),"Manager promotion bypassed history boundary");
        await service.RenameGroupAsync(group,"HR discussion");await service.RemoveMemberAsync(group,1);
        Act(1);await Denied(()=>service.ReadAsync(group));
        Act(3);await Invalid(()=>service.RemoveMemberAsync(group,3));await Invalid(()=>service.SetManagerAsync(group,3,false));
        await service.SetManagerAsync(group,2,true);await service.RemoveMemberAsync(group,3);
        Act(2);page=await service.ReadAsync(group);Check(page.CanManage&&page.Name=="HR discussion","Remaining manager lost ownership");
        await Denied(()=>service.GetRequestDetailsAsync(group,1));
        await service.AddMemberAsync(group,3);
        Act(3);Check(!(await service.ReadAsync(group)).CanManage,"Rejoining former manager regained manager permissions");
        Act(2);
        await using(var db=factory.CreateDbContext())
        {
            Check(await db.StaffChatMessages.CountAsync(m=>m.ConversationId==group)==6,"Membership changes deleted chat history");
            Check(await db.StaffChatMemberships.CountAsync(m=>m.ConversationId==group&&m.UserId==2)==2,"Membership history was overwritten");
            Check(await db.StaffChatEvents.AnyAsync(e=>e.ConversationId==group&&e.Action=="Removed")&&await db.StaffChatEvents.AnyAsync(e=>e.Action=="ManagerAdded"),"Group actions missing from retained audit history");
        }
        // Direct conversation conversion preserves its identity, messages and read state.
        Act(1);var direct=await service.StartAsync(2);var directMessage=await service.SendAsync(direct,"Original private chat",null,Guid.NewGuid());
        Act(2);await service.ReadAsync(direct);await service.ConvertToGroupAsync(direct,"Converted group");
        page=await service.ReadAsync(direct);Check(page.IsGroup&&page.CanManage&&page.Messages.Any(m=>m.Id==directMessage),"Direct conversion lost history");
        await service.AddMemberAsync(direct,3);
        Act(3);Check((await service.ReadAsync(direct)).Messages.Count==0,"Converted group's new member saw earlier direct messages");
        Act(2);await service.RemoveMemberAsync(direct,1);
        Act(1);await Denied(()=>service.ReadAsync(direct));
        var newDirect=await service.StartAsync(2);Check(newDirect!=direct&&!(await service.ReadAsync(newDirect)).IsGroup,"Converted group prevented a new private conversation");
        Act(5);await Denied(()=>service.ReadAsync(group));
    }
}
