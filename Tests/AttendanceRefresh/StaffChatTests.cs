using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class StaffChatTests
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Denied(Func<Task> action){try{await action();}catch(UnauthorizedAccessException){return;}throw new Exception("Expected chat access denial");}
    static async Task Invalid(Func<Task> action){try{await action();}catch(InvalidOperationException){return;}throw new Exception("Expected validation failure");}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),x=>x.EnableNullChecks(false))
            .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var user=1;var org=10;var person=100;
        void Act(int id){user=id;person=id*100;org=id==5?20:10;}
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{
            "RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=user,IndividualId=person,IsSuperAdministrator=user==4}),
            "GetCurrentOrganisationIdAsync"=>Task.FromResult(org),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! ==org),_=>throw new Exception(m)});
        var service=new StaffChatService(factory,access);var today=DateTime.UtcNow.AddHours(5).Date;
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation{BusinessEntityID=10,OrganisationName="Tenant"},new Organisation{BusinessEntityID=20,OrganisationName="Other"});
            db.UserOrganisations.AddRange(new UserOrganisation{UserOrganisationID=1,BusinessEntityID=10},new UserOrganisation{UserOrganisationID=2,BusinessEntityID=20});
            for(var id=1;id<=6;id++)
            {
                db.Users.Add(new(){UserId=id,BusinessEntityID=id*100,FullName=id==2?"HR colleague":"Staff "+id,Username="user"+id,
                    UserOrganisationId=id==5?2:1,IsActive=id!=6,LocalUserStateId=1,AdminPortalAccessEnabled=id==2,AdminPortalAccessOrganisationId=id==2?1:null});
                db.Individuals.Add(new(){BusinessEntityId=id*100,FirstNameEnglish="Staff",LastNameEnglish=id.ToString()});
                db.Jobs.Add(new(){JobId=id,IndividualID=id*100,OrganisationID=id==5?20:10,JobStateId=4,JoinedDate=today.AddYears(-1),OrganisationStructureId=1});
                db.JobPositions.Add(new(){JobPositionId=id,JobId=id,JobPositionStateId=4,FromDate=today.AddMonths(-1)});
            }
            db.OrganisationStructures.Add(new(){OrganisationStructureId=1,OrganisationBusinessEntityID=10,OrganisationStructureStateId=3,Name="Department"});
            db.OrganisationStructureSupervisors.Add(new(){OrganisationStructureId=1,SupervisorStaffId=300,IsValid=true,IsActive=true});
            db.UserGroups.Add(new(){UserGroupId=1,UserOrganisationId=1,GroupName="HR"});
            db.UserAssignedUserGroups.Add(new(){UserId=2,UserGroupId=1});
            db.Roles.Add(new(){RoleID=1,RoleKey="Requests.View",Name="View requests",ContextId=1});
            db.UserOrganisationRoles.Add(new(){UserOrganisationID=1,RoleID=1,IsActive=true});
            db.UserRoles.Add(new(){UserId=2,UserOrganisationId=1,RoleId=1,IsActive=true});
            db.RequestStates.AddRange(new RequestState{RequestStateId=2,StateName="Pending Verification",IsFinalState=false,IsProcessingState=true},
                new RequestState{RequestStateId=4,StateName="Approved",IsFinalState=true},new RequestState{RequestStateId=5,StateName="Cancelled",IsFinalState=true},
                new RequestState{RequestStateId=6,StateName="Rejected",IsFinalState=true});
            db.RequestTypes.Add(new(){RequestTypeId=1,TypeName="OT",ViewUrl="/online/overtime-requests"});
            db.Requests.AddRange(new Request{RequestId=1,RequestStateId=2,RequestTypeId=1,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=100,ReferenceNumber="OT-1",ApplicationDate=DateTime.UtcNow},
                new Request{RequestId=2,RequestStateId=2,RequestTypeId=1,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=400,ReferenceNumber="OT-2",ApplicationDate=DateTime.UtcNow},
                new Request{RequestId=3,RequestStateId=2,RequestTypeId=1,OrganisationBusinessEntityID=20,ApplicantBusinessEntityId=500,ReferenceNumber="OTHER-3",ApplicationDate=DateTime.UtcNow});
            db.RequestStates.Add(new(){RequestStateId=1,StateName="Incomplete",IsFinalState=false,IsProcessingState=false});
            db.RequestTypes.Add(new(){RequestTypeId=2,TypeName="Annual Leave Request",ViewUrl="/online/leaves"});
            foreach(var item in new[]{(Id:4,State:4,Reference:"ALR-APPROVED"),(Id:5,State:5,Reference:"ALR-CANCELLED"),(Id:6,State:6,Reference:"ALR-REJECTED"),(Id:7,State:2,Reference:"ALR-OPEN"),(Id:8,State:1,Reference:"ALR-INCOMPLETE")})
                db.Requests.Add(new(){RequestId=item.Id,RequestStateId=item.State,RequestTypeId=2,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=100,ReferenceNumber=item.Reference,ApplicationDate=DateTime.UtcNow});
            db.WfWorkflowDefinitions.Add(new(){WorkflowDefinitionId=1,RequestTypeId=1,OrganisationBusinessEntityId=10,Name="Supervisor"});
            db.WfWorkflowStages.Add(new(){WorkflowStageId=1,WorkflowDefinitionId=1,StageOrder=1,StageName="Supervisor",RequestStateId=2});
            db.WfStageApprovers.Add(new(){StageApproverId=1,WorkflowStageId=1,ApproverType="Supervisor",ApproverReferenceId=0});
            db.WfRequestInstances.Add(new(){WorkflowRequestInstanceId=1,RequestId=1,WorkflowDefinitionId=1});
            db.WfRequestStageInstances.Add(new(){RequestStageInstanceId=1,WorkflowRequestInstanceId=1,WorkflowStageId=1});
            db.WfRequestApprovals.Add(new(){RequestApprovalId=1,RequestStageInstanceId=1,ApproverUserId=3,StageApproverId=1});
            await db.SaveChangesAsync();
        }
        var contacts=await service.SearchContactsAsync("");
        Check(contacts.Any(c=>c.UserId==2&&c.IsHr)&&contacts.Any(c=>c.UserId==3&&c.IsSupervisor)&&!contacts.Any(c=>c.UserId is 1 or 5 or 6),"Directory scope/HR/supervisor labels failed");
        Check((await service.SearchContactsAsync("","hr")).Select(c=>c.UserId).SequenceEqual([2]),"HR filter failed");
        Check((await service.SearchContactsAsync("","supervisor")).Select(c=>c.UserId).SequenceEqual([3]),"Supervisor filter failed");
        await Denied(()=>service.StartAsync(5));await Denied(()=>service.StartAsync(6));await Denied(()=>service.StartAsync(1));
        var chat=await service.StartAsync(2);Check(await service.StartAsync(2)==chat,"Duplicate conversation created");
        var key=Guid.NewGuid();var message=await service.SendAsync(chat,"Hello HR",null,key);
        Check(await service.SendAsync(chat,"Hello HR",null,key)==message,"Client retry duplicated a message");
        await Invalid(()=>service.SendAsync(chat,"Changed payload",null,key));
        await Invalid(()=>service.SendAsync(chat,new string('x',4001),null,Guid.NewGuid()));
        var references=await service.SearchRequestsAsync(chat,"OT");
        Check(references.Count==1&&references[0].Id==1,"Request search disclosed another employee or tenant request");
        var annual=await service.SearchRequestsAsync(chat,"Annual Leave");
        Check(annual.Select(r=>r.Id).Order().SequenceEqual([7,8]),"Request type search included final states or excluded non-processing Incomplete");
        Check((await service.SearchRequestsAsync(chat,"Staff 1")).Any(r=>r.Id==7),"Applicant name search failed");
        Check((await service.SearchRequestsAsync(chat,"ALR-OPEN")).Select(r=>r.Id).SequenceEqual([7]),"Exact reference search failed");
        Check(!(await service.SearchRequestsAsync(chat,"")).Any(r=>r.Id is 4 or 5 or 6),"Final requests appeared in attachment choices");
        var statuses=await service.GetRequestStatusesAsync(chat);
        Check(statuses.Any(s=>s.Id==1)&&statuses.Any(s=>s.Id==2)&&!statuses.Any(s=>s.Id is 4 or 5 or 6),"Status choices did not preserve open/non-final eligibility");
        var filterDate=today.AddDays(-3);
        var firstUtc=filterDate.AddHours(-5);var nextUtc=filterDate.AddDays(1).AddHours(-5);
        await using(var db=factory.CreateDbContext())
        {
            (await db.Requests.SingleAsync(r=>r.RequestId==7)).ApplicationDate=firstUtc;
            (await db.Requests.SingleAsync(r=>r.RequestId==8)).ApplicationDate=nextUtc.AddTicks(-1);
            db.Requests.AddRange(new Request{RequestId=9,RequestStateId=2,RequestTypeId=2,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=100,ReferenceNumber="ALR-PREVIOUS",ApplicationDate=firstUtc.AddTicks(-1)},
                new Request{RequestId=10,RequestStateId=2,RequestTypeId=2,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=100,ReferenceNumber="ALR-NEXT",ApplicationDate=nextUtc});
            await db.SaveChangesAsync();
        }
        Check((await service.SearchRequestsAsync(chat,"Annual",filterDate,filterDate)).Select(r=>r.Id).Order().SequenceEqual([7,8]),"Submitted date boundaries were not inclusive in Maldives local time");
        Check((await service.SearchRequestsAsync(chat,"Annual",filterDate,filterDate,2)).Select(r=>r.Id).SequenceEqual([7]),"Combined text/date/status filters failed");
        Check((await service.SearchRequestsAsync(chat,"Annual",filterDate,filterDate,1)).Select(r=>r.Id).SequenceEqual([8]),"Incomplete status filter failed");
        Check((await service.SearchRequestsAsync(chat,"Annual",filterDate.AddDays(1))).Select(r=>r.Id).SequenceEqual([10]),"From-only filter failed");
        Check((await service.SearchRequestsAsync(chat,"Annual",toDate:filterDate.AddDays(-1))).Select(r=>r.Id).SequenceEqual([9]),"To-only filter failed");
        await Invalid(()=>service.SearchRequestsAsync(chat,"",filterDate.AddDays(1),filterDate));
        await Invalid(()=>service.SearchRequestsAsync(chat,"",DateTime.MinValue));
        await Invalid(()=>service.SearchRequestsAsync(chat,"",statusId:4));
        await Invalid(()=>service.SearchRequestsAsync(chat,"",statusId:999));
        foreach(var id in new[]{4,5,6})await Invalid(()=>service.SendAsync(chat,"Final request",id,Guid.NewGuid()));
        await service.SendAsync(chat,"About my request",1,Guid.NewGuid());
        await Denied(()=>service.SendAsync(chat,"Other request",2,Guid.NewGuid()));
        await Denied(()=>service.SendAsync(chat,"Other tenant",3,Guid.NewGuid()));
        Act(2);Check(await service.StartAsync(1)==chat,"Reverse participants created another conversation");
        Check((await service.ListAsync()).Single().Unread==2,"Unread messages not counted");
        await using(var db=factory.CreateDbContext())Check(await db.StaffNotifications.CountAsync(n=>n.UserId==2&&n.Kind=="Chat")==1,"Chat notifications were not coalesced");
        await service.ReadAsync(chat,markRead:false);
        Check((await service.ListAsync()).Single().Unread==2,"Background/non-reading refresh cleared unread messages");
        var page=await service.ReadAsync(chat);Check(page.Messages.Count==2&&page.Messages.Last().Request?.Reference=="OT-1","Message history/request attachment missing");
        Check((await service.ListAsync()).Single().Unread==0,"Reading did not clear unread messages");
        await using(var db=factory.CreateDbContext())Check(await db.StaffNotifications.AllAsync(n=>n.UserId!=2||n.ReadAtUtc.HasValue),"Reading did not clear chat bell alert");
        await service.SendAsync(chat,"I will check",null,Guid.NewGuid());
        Act(4);Check((await service.ListAsync()).Count==0,"Administrator could browse private conversations");
        await Denied(()=>service.ReadAsync(chat));await Denied(()=>service.SendAsync(chat,"Intrusion",null,Guid.NewGuid()));await Denied(()=>service.SearchRequestsAsync(chat,"OT"));await Denied(()=>service.GetRequestStatusesAsync(chat));
        Act(5);await Denied(()=>service.ReadAsync(chat));
        Act(1);var supervisorChat=await service.StartAsync(3);
        await service.SendAsync(supervisorChat,"Request for my supervisor",1,Guid.NewGuid());
        Act(3);await Denied(()=>service.GetRequestDetailsAsync(chat,1));
        Act(3);Check((await service.ReadAsync(supervisorChat)).Messages.Single().Request?.Url!=null,"Current supervisor did not receive a private request link");
        Check((await service.GetRequestDetailsAsync(supervisorChat,1)).Reference=="OT-1","Current supervisor could not open protected details");
        await using(var db=factory.CreateDbContext()){(await db.OrganisationStructureSupervisors.SingleAsync()).IsActive=false;await db.SaveChangesAsync();}
        var removedReference=(await service.ReadAsync(supervisorChat)).Messages.Single().Request;
        Check(removedReference?.Id==1&&removedReference.Url==null,"Removed supervisor lost the information card or retained a private request link");
        await Denied(()=>service.GetRequestDetailsAsync(supervisorChat,1));
        Act(1);var other=await service.StartAsync(4);await service.SendAsync(other,"Shared reference information",1,Guid.NewGuid());
        Act(4);var information=(await service.ReadAsync(other)).Messages.Single().Request;
        Check(information?.Reference=="OT-1"&&information.Url==null,"Unrelated recipient did not receive an information-only reference");
        await Denied(()=>service.GetRequestDetailsAsync(other,1));
        Act(1);Check((await service.GetRequestDetailsAsync(other,1)).Reference=="OT-1","Requester could not open protected details");
        await Denied(()=>service.GetRequestDetailsAsync(other,7));
        // Role changes do not erase information already shared; they never grant private detail access.
        await using(var db=factory.CreateDbContext()){(await db.UserRoles.SingleAsync()).IsActive=false;await db.SaveChangesAsync();}
        Act(2);page=await service.ReadAsync(chat);
        Check(page.Messages.Any(m=>m.Request?.Reference=="OT-1")&&page.Messages.Where(m=>m.Request!=null).All(m=>m.Request!.Url==null),"Shared information disappeared or unrelated HR received a private link");
        await Denied(()=>service.GetRequestDetailsAsync(chat,1));
        Act(1);
        await using(var db=factory.CreateDbContext())
        {
            (await db.RequestTypes.SingleAsync(t=>t.RequestTypeId==1)).ViewUrl="https://external.invalid/";
            (await db.UserRoles.SingleAsync()).IsActive=true;
            for(var i=0;i<55;i++)db.StaffChatMessages.Add(new(){ConversationId=chat,SenderUserId=2,ClientMessageId=Guid.NewGuid(),Body="History "+i,SentAtUtc=DateTime.UtcNow.AddDays(-1)});
            await db.SaveChangesAsync();
        }
        page=await service.ReadAsync(chat);Check(page.Messages.Count==50&&page.HasOlder,"Chat history not paged");
        var older=await service.ReadAsync(chat,page.Messages.Min(m=>m.Id),false);
        Check(older.Messages.Count>0&&!older.Messages.IntersectBy(page.Messages.Select(m=>m.Id),m=>m.Id).Any(),"Older message pagination overlaps");
        Check(older.Messages.Where(m=>m.Request!=null).All(m=>m.Request!.Url==$"/online/chat/{chat}/requests/{m.Request.Id}"),"Request link bypassed the protected chat detail route");
        await using(var db=factory.CreateDbContext()){(await db.Requests.SingleAsync(r=>r.RequestId==1)).RequestStateId=4;await db.SaveChangesAsync();}
        Check(!(await service.SearchRequestsAsync(chat,"OT-1")).Any(),"A request that became final remained selectable");
        await Invalid(()=>service.SendAsync(chat,"Stale selection that became final",1,Guid.NewGuid()));
        Check((await service.ReadAsync(chat,page.Messages.Min(m=>m.Id),false)).Messages.Any(m=>m.Request?.Id==1),"Finalisation removed an existing reference from chat history");
        await using(var db=factory.CreateDbContext()){(await db.JobPositions.SingleAsync(p=>p.JobId==2)).ToDate=today;await db.SaveChangesAsync();}
        Check(!(await service.ReadAsync(chat)).CanSend,"Ended position still allowed sending");
        await Invalid(()=>service.SendAsync(chat,"Unavailable colleague",null,Guid.NewGuid()));
        // Rate limiting and validation are enforced by the server, not just the form.
        await using(var db=factory.CreateDbContext())
        {
            for(var i=0;i<30;i++)db.StaffChatMessages.Add(new(){ConversationId=other,SenderUserId=1,ClientMessageId=Guid.NewGuid(),Body="Rate test",SentAtUtc=DateTime.UtcNow});
            await db.SaveChangesAsync();
        }
        await Invalid(()=>service.SendAsync(other,"Too many messages",null,Guid.NewGuid()));
        await Invalid(()=>service.SendAsync(other," ",null,Guid.NewGuid()));
        Act(2);await Denied(()=>service.ListAsync());
        Act(1);person=400;await Denied(()=>service.ListAsync());
    }
}
