using HRM.DTOs;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class SupervisorWorkflowTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Denied(Func<Task> action){try{await action();}catch(UnauthorizedAccessException){return;}throw new Exception("Expected assignment/tenant denial");}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),o=>o.EnableNullChecks(false)).ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var actor=new UserContextDto{UserId=3,IndividualId=300};var org=10;
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(actor),"GetCurrentOrganisationIdAsync"=>Task.FromResult(org),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! ==org),"HasPermissionAsync"=>Task.FromResult(true),_=>throw new Exception(m)});
        var logs=Proxy.For<IOperationLogService>((m,a)=>m=="CreateAsync"?Audit((HrmTeContext)a[0]!):throw new Exception(m));
        var create=Proxy.For<IRequestService>((m,a)=>m=="CreateAsync"?Create((HrmTeContext)a[0]!,(CreateRequestDto)a[1]!):throw new Exception(m));
        var workflow=Proxy.For<IWorkflowEngine>((m,a)=>m=="StartAsync"?Start((HrmTeContext)a[0]!,Convert.ToInt32(a[1])):throw new Exception(m));
        var resolver=Proxy.For<IWorkflowApproverResolver>((m,a)=>Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>([new((int)a[0]!%10==1?1:4,1,1,true)]));
        var contexts=new WorkflowRequestContextResolver(factory);
        var service=new SupervisorChangeRequestService(factory,access,create,logs,workflow,resolver,contexts);
        var today=DateTime.UtcNow.AddHours(5).Date;
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.Add(new(){BusinessEntityID=10,OrganisationName="Tenant"});db.UserOrganisations.Add(new(){UserOrganisationID=1,BusinessEntityID=10});
            foreach(var p in new[]{100,200,300,400})db.Individuals.Add(new(){BusinessEntityId=p,FirstNameEnglish=$"Staff {p}"});
            foreach(var pair in new[]{(1,200),(2,100),(3,300),(4,400)})db.Users.Add(new(){UserId=pair.Item1,BusinessEntityID=pair.Item2,UserOrganisationId=1,Username=$"user{pair.Item1}",IsActive=true,LocalUserStateId=1});
            db.Staffs.Add(new(){IndividualId=100});db.JobTypes.Add(new(){JobTypeId=1,TypeName="Contract"});db.Jobs.Add(new(){JobId=9,IndividualID=100,OrganisationID=10,JobTypeId=1,JobStateId=4,JoinedDate=today.AddDays(-5)});
            db.OrganisationStructures.Add(new(){OrganisationStructureId=5,OrganisationBusinessEntityID=10,OrganisationStructureStateId=3,Name="Department"});db.Teams.Add(new(){TeamId=7,OrganisationId=10,Name="Team",IsValid=true,StartDate=today.AddDays(-1)});
            for(var state=1;state<=6;state++)db.RequestStates.Add(new(){RequestStateId=state,StateName=$"State {state}",IsProcessingState=state<4,IsFinalState=state>=4});
            for(var type=1;type<=2;type++)
            {
                db.RequestTypes.Add(new(){RequestTypeId=type,TypeName=SupervisorChangeRequestService.TypeName(type==1),ViewUrl=SupervisorChangeRequestService.PageUrl});
                foreach(var pair in new[]{(2,3),(3,4),(2,5),(2,6)})db.RequestTypesAllowedRequestStateTransitions.Add(new(){RequestTypeId=type,FromRequestStateId=pair.Item1,ToRequestStateId=pair.Item2});
                db.WfWorkflowDefinitions.Add(new(){WorkflowDefinitionId=type,RequestTypeId=type,OrganisationBusinessEntityId=10,Name="Two stages",Stages=[new(){WorkflowStageId=type*10+1,StageOrder=1,StageName="First",RequestStateId=2,ApprovalMode="Any"},new(){WorkflowStageId=type*10+2,StageOrder=2,StageName="Final",RequestStateId=3,ApprovalMode="Any"}]});
            }
            await db.SaveChangesAsync();
        }
        async Task<int> Submit(string kind,bool assign)
        {
            actor.UserId=3;actor.IndividualId=300;
            var result=await service.SubmitAsync(10,kind,kind=="Team"?7:5,100,assign,"Reviewed business need");Check(result.Success,"Submission failed: "+result.Message);
            await using var db=factory.CreateDbContext();return await db.SupervisorChangeRequests.Where(r=>r.IsOpen).OrderByDescending(r=>r.RequestId).Select(r=>r.RequestId).FirstAsync();
        }
        async Task Approve(int id)
        {
            actor.UserId=1;actor.IndividualId=200;Check((await service.ActAsync(id,2,"Approve","Stage one")).Success,"First approval failed");
            actor.UserId=4;actor.IndividualId=400;Check((await service.ActAsync(id,3,"Approve","Final review")).Success,"Final approval failed");
        }
        var id=await Submit("Department",true);
        Check(!(await service.SubmitAsync(10,"Department",5,100,true,"Duplicate")).Success,"Duplicate pending assignment accepted");
        await using(var db=factory.CreateDbContext())Check(!await db.OrganisationStructureSupervisors.AnyAsync(),"Submission assigned the supervisor before approval");
        actor.UserId=1;actor.IndividualId=200;
        await using(var db=factory.CreateDbContext()){db.RequestTypesAllowedRequestStateTransitions.Remove(await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(t=>t.RequestTypeId==1&&t.FromRequestStateId==2&&t.ToRequestStateId==3));await db.SaveChangesAsync();}
        Check(!(await service.ActAsync(id,2,"Approve","Blocked transition")).Success,"Missing state transition was bypassed");
        await using(var db=factory.CreateDbContext()){db.RequestTypesAllowedRequestStateTransitions.Add(new(){RequestTypeId=1,FromRequestStateId=2,ToRequestStateId=3});await db.SaveChangesAsync();}
        actor.UserId=3;actor.IndividualId=300;await Denied(()=>service.ActAsync(id,2,"Approve","Unassigned Admin"));
        actor.UserId=2;actor.IndividualId=100;await Denied(()=>service.ActAsync(id,2,"Approve","Self approval"));
        actor.UserId=1;actor.IndividualId=200;Check((await service.ActAsync(id,2,"Approve","First")).Success,"First stage failed");
        await using(var db=factory.CreateDbContext())Check(!await db.OrganisationStructureSupervisors.AnyAsync(),"Intermediate approval applied assignment");
        actor.UserId=4;actor.IndividualId=400;Check((await service.ActAsync(id,3,"Approve","Final")).Success,"Final assignment failed");
        Check(!(await service.ActAsync(id,3,"Approve","Repeat")).Success,"Completed request applied twice");
        await using(var db=factory.CreateDbContext())Check(await db.OrganisationStructureSupervisors.AnyAsync(s=>s.IsActive&&s.IsValid)&&!(await db.SupervisorChangeRequests.FindAsync(id))!.IsOpen,"Final approval did not retain the assignment/close request");
        id=await Submit("Department",false);actor.UserId=1;actor.IndividualId=200;Check((await service.ActAsync(id,2,"Reject","Keep supervisor")).Success,"Removal rejection failed");
        await using(var db=factory.CreateDbContext())Check(await db.OrganisationStructureSupervisors.AnyAsync(s=>s.IsActive),"Rejected removal changed assignment");
        id=await Submit("Department",false);Check((await service.ActAsync(id,2,"Cancel","Withdraw")).Success,"Submitting officer could not cancel");
        id=await Submit("Department",false);await Approve(id);
        await using(var db=factory.CreateDbContext())Check(!await db.OrganisationStructureSupervisors.AnyAsync(s=>s.IsActive),"Approved removal left department supervisor active");
        id=await Submit("Team",true);await Approve(id);
        await using(var db=factory.CreateDbContext())Check(await db.TeamStaffs.AnyAsync(s=>s.IsSuperVisor&&s.IsValid),"Team assignment not applied");
        id=await Submit("Team",false);
        await using(var db=factory.CreateDbContext()){var member=await db.TeamStaffs.SingleAsync();member.OperationLogId++;await db.SaveChangesAsync();}
        actor.UserId=1;actor.IndividualId=200;Check(!(await service.ActAsync(id,2,"Approve","Stale")).Success,"Stale assignment change was approved");
        actor.UserId=3;actor.IndividualId=300;Check((await service.ActAsync(id,2,"Cancel","Refresh request")).Success,"Stale request could not be canceled");
        id=await Submit("Team",false);await Approve(id);
        await using(var db=factory.CreateDbContext())Check(!await db.TeamStaffs.AnyAsync(s=>s.IsSuperVisor)&&await db.TeamStaffs.AnyAsync(s=>s.IsValid),"Removal deleted team membership or retained supervisor role");
        org=20;await Denied(()=>service.ActAsync(id,4,"Approve","Foreign",10));await Denied(()=>service.LoadAsync(10));
        org=10;Check(!(await new OrganisationSettingsService(factory,access,logs).SetTeamSupervisorAsync(10,"Team",7,100,true)).Success,"Legacy setter bypassed approval");
        await using(var db=factory.CreateDbContext())Check(await db.RequestNotifications.AnyAsync(n=>n.UserId==2)&&await db.RequestNotifications.AnyAsync(n=>n.UserId==3)&&await db.RequestNotifications.AnyAsync(n=>n.UserId==4),"Subject, submitting officer or next-stage notification was not recorded");
    }
    static async Task<OperationLog> Audit(HrmTeContext db){var log=new OperationLog();db.OperationLogs.Add(log);await db.SaveChangesAsync();return log;}
    static async Task<Request> Create(HrmTeContext db,CreateRequestDto dto){var r=new Request{RequestTypeId=dto.RequestTypeId,RequestStateId=2,ApplicantBusinessEntityId=dto.ApplicantBusinessEntityId,OrganisationBusinessEntityID=dto.OrganisationBusinessEntityId};db.Requests.Add(r);await db.SaveChangesAsync();return r;}
    static async Task<long> Start(HrmTeContext db,int id)
    {
        var request=await db.Requests.FindAsync(id);var type=request!.RequestTypeId;
        var instance=new WfRequestInstance{RequestId=id,WorkflowDefinitionId=type};db.WfRequestInstances.Add(instance);await db.SaveChangesAsync();
        var stage=new WfRequestStageInstance{WorkflowRequestInstanceId=instance.WorkflowRequestInstanceId,WorkflowStageId=type*10+1,Approvals=[new(){ApproverUserId=1,Decision="Pending"}]};db.WfRequestStageInstances.Add(stage);await db.SaveChangesAsync();instance.CurrentRequestStageInstanceId=stage.RequestStageInstanceId;await db.SaveChangesAsync();return instance.WorkflowRequestInstanceId;
    }
}
