using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class HandoverRuleTests
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Invalid(Func<Task> action){try{await action();}catch(InvalidOperationException){return;}throw new Exception("Expected invalid rule rejection");}
    static async Task Denied(Func<Task> action){try{await action();}catch(UnauthorizedAccessException){return;}throw new Exception("Expected scope/permission denial");}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),x=>x.EnableNullChecks(false)).ConfigureWarnings(x=>x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var actor=new UserContextDto{UserId=1,IndividualId=100};var permission=true;var day=new DateTime(2026,10,26);
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(actor),"GetCurrentOrganisationIdAsync"=>Task.FromResult(10),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! ==10),"HasPermissionAsync"=>Task.FromResult(permission&&(string)a[0]! =="Admin"),_=>throw new Exception(m)});
        var logs=Proxy.For<IOperationLogService>((m,a)=>m=="CreateAsync"?Audit((HrmTeContext)a[0]!):throw new Exception(m));
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.AddRange(new(){BusinessEntityID=10,OrganisationName="Current"},new(){BusinessEntityID=20,OrganisationName="Other"});
            db.JobTypes.AddRange(new(){JobTypeId=1,TypeName="Contract",IsActive=true},new(){JobTypeId=2,TypeName="Permanent",IsActive=true});
            db.LeaveTypes.AddRange(new(){LeaveTypeId=1,Name="Annual",OrganisationId=20,IsGlobal=true},new(){LeaveTypeId=66,Name="Annual local",OrganisationId=10},new(){LeaveTypeId=99,Name="Foreign",OrganisationId=20});
            db.OrganisationStructures.AddRange(new(){OrganisationStructureId=101,OrganisationBusinessEntityID=10,Name="Department A",OrganisationStructureStateId=3},new(){OrganisationStructureId=102,OrganisationBusinessEntityID=10,Name="Department B",OrganisationStructureStateId=3},new(){OrganisationStructureId=103,OrganisationBusinessEntityID=10,Name="Unapproved",OrganisationStructureStateId=1},new(){OrganisationStructureId=201,OrganisationBusinessEntityID=20,Name="Foreign",OrganisationStructureStateId=3});
            db.Teams.AddRange(new(){TeamId=501,OrganisationId=10,Name="Team A",IsValid=true},new(){TeamId=502,OrganisationId=10,Name="Team B",IsValid=true},new(){TeamId=599,OrganisationId=20,Name="Foreign",IsValid=true});
            db.Jobs.AddRange(new(){JobId=1,IndividualID=100,OrganisationID=10,OrganisationStructureId=101,JobTypeId=1,JobStateId=4},new(){JobId=2,IndividualID=200,OrganisationID=20,OrganisationStructureId=201,JobTypeId=1,JobStateId=4},new(){JobId=3,IndividualID=300,OrganisationID=10,OrganisationStructureId=101,JobTypeId=2,JobStateId=4},new(){JobId=4,IndividualID=400,OrganisationID=10,OrganisationStructureId=102,JobTypeId=1,JobStateId=4});
            db.TeamStaffs.AddRange(new(){TeamId=501,StaffId=100,IsValid=true,StartDate=day.AddDays(-10)},new(){TeamId=501,StaffId=300,IsValid=true,StartDate=day.AddDays(-10)});
            await db.SaveChangesAsync();
            Check(db.Model.FindEntityType(typeof(LeaveHandoverRule))!.GetTableName()=="LH_LeaveHandoverRules"&&db.Model.FindEntityType(typeof(LeaveHandoverRuleType))!.GetTableName()=="LH_LeaveHandoverRuleTypes","Handover tables do not follow naming convention");
        }
        var service=new LeaveHandoverRuleSettingsService(factory,access,logs);
        HandoverRuleEdit Rule(string name,string scope="Organisation")=>new(){OrganisationId=10,Name=name,Scope=scope,AfterDays=2,EffectiveFrom=day.AddDays(-10)};
        await service.SaveAsync(Rule("Default"));
        async Task<EffectiveHandoverRule> Resolve(int job=1,int type=66,DateTime? date=null){await using var db=factory.CreateDbContext();return await LeaveHandoverRuleResolver.ResolveAsync(db,job,type,date??day);}
        var r=await Resolve(4);Check(!r.Requires(2)&&r.Requires(3)&&r.AfterDays==2,"More-than-X boundary is wrong");
        actor.ActiveJob=new ActiveJobDto{JobId=1,OrganisationId=10};
        await using(var db=factory.CreateDbContext())
        {
            (await db.LeaveTypes.SingleAsync(x=>x.LeaveTypeId==66)).RequestTypeId=34;
            db.RequestTypes.Add(new HRM.Models.Archives.RequestType{RequestTypeId=34,TypeName="Annual Leave Request"});
            db.LeaveTypeReasonTypes.Add(new(){LeaveTypeReasonTypeId=7,LeaveTypeId=66,ReasonTypeName="Other"});
            db.JobLeaveTypes.Add(new(){JobLeaveTypeId=1000,JobId=1,LeaveTypeId=66,IsValid=true,IsLeaveInfoUpdated=true,RemainingDays=30});
            await db.SaveChangesAsync();
        }
        var applications=new LeaveService(factory,logs,null!,null!,access,null!,null!);
        var uiRule=await applications.GetHandoverRulesAsync(1000,day);
        Check(uiRule.AfterDays==2&&uiRule.RuleId.HasValue,"Application UI did not resolve the same scoped rule as the server");
        var blocked=await applications.SubmitLeave(new(){JobLeaveTypeId=1000,LeaveTypeId=66,LeaveReasonId=7,FromDate=day,ToDate=day.AddDays(2),Duration=3});
        Check(!blocked.Success&&blocked.Message.Contains("handover tasks",StringComparison.OrdinalIgnoreCase),"Server allowed a client to bypass required handover");
        await using(var db=factory.CreateDbContext())Check(await db.Leaves.CountAsync()==0&&(await db.JobLeaveTypes.SingleAsync(x=>x.JobLeaveTypeId==1000)).RemainingDays==30,"Blocked handover changed a balance or created a leave");
        var department=Rule("Department","Structure");department.OrganisationStructureId=101;department.AllLeaveTypes=false;department.LeaveTypeIds=[1,66];department.AfterDays=4;await service.SaveAsync(department);
        Check((await Resolve()).AfterDays==4&&(await Resolve(4)).AfterDays==2,"Department override or organisation fallback failed");
        var team=Rule("Team","Team");team.TeamId=501;team.AllLeaveTypes=false;team.LeaveTypeIds=[66];team.JobTypeId=1;team.Mode="Exempt";await service.SaveAsync(team);
        Check((await Resolve()).Mode=="Exempt"&&(await Resolve(3)).AfterDays==4&&(await Resolve(1,1)).AfterDays==4,"Team, Job Type or selected-type filtering failed");
        team=(await service.LoadAsync()).Rules.Single(x=>x.Name=="Team");team.Mode="Inherit";await service.SaveAsync(team);Check((await Resolve()).AfterDays==4,"Inherit did not fall back to department");
        team=(await service.LoadAsync()).Rules.Single(x=>x.Name=="Team");team.Mode="Required";team.AfterDays=0;await service.SaveAsync(team);r=await Resolve();Check(r.Requires(1),"Zero-day threshold did not require all leave");
        var frozen=r.Snapshot(3);await using(var db=factory.CreateDbContext()){db.Leaves.Add(new(){LeaveId=99,JobId=1,LeaveTypeId=66,Duration=3,HandoverRuleSnapshot=frozen});await db.SaveChangesAsync();}
        team=(await service.LoadAsync()).Rules.Single(x=>x.Name=="Team");var stale=System.Text.Json.JsonSerializer.Deserialize<HandoverRuleEdit>(System.Text.Json.JsonSerializer.Serialize(team))!;team.AfterDays=9;await service.SaveAsync(team);await Invalid(()=>service.SaveAsync(stale));
        await using(var db=factory.CreateDbContext())Check((await db.Leaves.SingleAsync(x=>x.LeaveId==99)).HandoverRuleSnapshot==frozen,"Rule edit changed an application snapshot");
        var duplicate=Rule("Duplicate","Team");duplicate.TeamId=501;duplicate.AllLeaveTypes=false;duplicate.LeaveTypeIds=[66];duplicate.JobTypeId=1;await Invalid(()=>service.SaveAsync(duplicate));
        var second=Rule("Second team","Team");second.TeamId=502;second.AllLeaveTypes=false;second.LeaveTypeIds=[66];second.JobTypeId=1;second.Mode="Exempt";await service.SaveAsync(second);
        await using(var db=factory.CreateDbContext()){db.TeamStaffs.Add(new(){TeamId=502,StaffId=100,StartDate=day.AddDays(-1),IsValid=true});await db.SaveChangesAsync();}
        try{await Resolve();throw new Exception("Multiple-team ambiguity was silently resolved");}catch(HandoverRuleConflictException){}
        second=(await service.LoadAsync()).Rules.Single(x=>x.Name=="Second team");second.Priority=10;await service.SaveAsync(second);Check((await Resolve()).Mode=="Exempt","Explicit priority did not resolve overlapping teams");
        second=(await service.LoadAsync()).Rules.Single(x=>x.Name=="Second team");second.IsActive=false;await service.SaveAsync(second);
        await using(var db=factory.CreateDbContext()){var member=await db.TeamStaffs.SingleAsync(x=>x.TeamId==501&&x.StaffId==100);member.EndDate=day.AddDays(-1);await db.SaveChangesAsync();}
        Check((await Resolve()).AfterDays==4,"Expired membership still applied a team override");
        var future=Rule("Future");future.Priority=10;future.EffectiveFrom=day.AddDays(7);future.AfterDays=5;await service.SaveAsync(future);Check((await Resolve(4)).AfterDays==2&&(await Resolve(4,date:day.AddDays(8))).AfterDays==5,"Effective dates were not respected");
        var foreign=Rule("Foreign team","Team");foreign.TeamId=599;await Invalid(()=>service.SaveAsync(foreign));var unapproved=Rule("Unapproved","Structure");unapproved.OrganisationStructureId=103;await Invalid(()=>service.SaveAsync(unapproved));var foreignType=Rule("Foreign type");foreignType.AllLeaveTypes=false;foreignType.LeaveTypeIds=[99];await Invalid(()=>service.SaveAsync(foreignType));
        await Denied(()=>service.PreviewAsync(2,66,day));await Denied(()=>service.PreviewAsync(1,99,day));
        var data=await service.LoadAsync();Check(data.Structures.All(x=>x.Id is 101 or 102)&&data.Teams.All(x=>x.Id is 501 or 502)&&data.Types.All(x=>x.Id is 1 or 66),"Settings leaked foreign or unapproved options");
        var wrongContext=Rule("Wrong context");wrongContext.OrganisationId=20;await Denied(()=>service.SaveAsync(wrongContext));
        permission=false;await Denied(()=>service.LoadAsync());actor.IsSuperAdministrator=true;Check((await service.LoadAsync()).Organisation=="Current","Super Administrator lost current tenant scope");
    }
    static async Task<OperationLog> Audit(HrmTeContext db){var log=new OperationLog();db.OperationLogs.Add(log);await db.SaveChangesAsync();return log;}
}
