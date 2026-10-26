using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class SystemGapTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Denied(Func<Task> action){try{await action();}catch(UnauthorizedAccessException){return;}throw new Exception("Expected permission/tenant denial");}
    public static async Task RunAsync()
    {
        var boundary=DateTime.UtcNow.AddHours(5).Date;
        Check(PositionLifecycle.Contains(boundary.AddDays(-1),boundary.AddDays(1),boundary),"Future position end excluded today");
        Check(!PositionLifecycle.Contains(boundary.AddDays(-1),boundary,boundary)&&!PositionLifecycle.Contains(boundary.AddDays(-2),boundary.AddDays(-1),boundary),"Ended position remained current");
        Check(!PositionLifecycle.Contains(boundary.AddDays(1),null,boundary)&&PositionLifecycle.Contains(boundary,null,boundary.AddHours(23)),"Position start/midnight boundary mismatch");
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),o=>o.EnableNullChecks(false)).ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var actor=new UserContextDto{UserId=1,IndividualId=100};var permissions=true;
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(actor),"GetCurrentOrganisationIdAsync"=>Task.FromResult(10),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! ==10),"HasPermissionAsync"=>Task.FromResult(permissions&&(string)a[0]! =="Admin"),_=>throw new Exception(m)});
        var logs=Proxy.For<IOperationLogService>((m,a)=>m=="CreateAsync"?Audit((HrmTeContext)a[0]!):throw new Exception(m));
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation{BusinessEntityID=10,OrganisationName="Tenant"},new Organisation{BusinessEntityID=20,OrganisationName="Other"});
            db.UserOrganisations.Add(new(){UserOrganisationID=1,BusinessEntityID=10});
            db.Users.Add(new(){UserId=1,BusinessEntityID=100,UserOrganisationId=1,Username="reviewer",IsActive=true,LocalUserStateId=1});
            db.Individuals.AddRange(new Individual{BusinessEntityId=100,FirstNameEnglish="HR"},new Individual{BusinessEntityId=200,FirstNameEnglish="Staff"});
            db.BusinessEntityTypes.Add(new(){BusinessEntityTypeId=1,TypeName="Individual"});db.Countries.Add(new(){CountryId=1,NameEnglish="Maldives"});db.IdcardStates.Add(new(){IdcardStateId=1,StateName="Pending"});
            db.JobTypes.Add(new(){JobTypeId=1,TypeName="Contract",IsActive=true});
            db.OrganisationStructureTypes.Add(new(){OrganisationStructureTypeId=1,TypeName="Department"});
            db.OrganisationStructures.Add(new(){OrganisationStructureId=1,OrganisationBusinessEntityID=10,OrganisationStructureStateId=3,OrganisationStructureTypeId=1,Name="Approved department"});
            db.GroupTypes.Add(new(){GroupTypeId=1,OrganisationBusinessEntityId=10,Name="Team"});
            db.LeaveConfigurableValueTypes.Add(new(){LeaveConfigurableValueTypeId=1,Name="Limit",Type="number",Description="Limit"});
            db.LeaveConfigurableValues.Add(new(){LeaveConfigurableValueId=20,OrganisationId=20,LeaveConfigurableValueTypeId=1,Value="99"});
            db.RequestTypes.Add(new(){RequestTypeId=100,TypeName="Position"});db.RequestStates.AddRange(new RequestState{RequestStateId=2,StateName="Pending",IsProcessingState=true},new RequestState{RequestStateId=4,StateName="Approved",IsFinalState=true});
            db.Jobs.Add(new(){JobId=9,IndividualID=200,OrganisationID=10,JobTypeId=1,JobStateId=4,JoinedDate=DateTime.UtcNow.AddHours(5).Date.AddYears(-1)});
            db.Positions.Add(new(){PositionId=1,Name="Officer",OrganisationId=10,IsActive=true});await db.SaveChangesAsync();
        }
        var leave=new LeaveConfigurationSettingsService(factory,access,logs);
        Check((await leave.LoadAsync()).Values.Count==0,"Leave configuration leaked another tenant");
        await Denied(()=>leave.DeleteAsync(20));await Denied(()=>leave.SaveAsync(new(){OrganisationId=20,LeaveConfigurableValueTypeId=1,Value="1"}));
        await leave.SaveAsync(new(){OrganisationId=10,LeaveConfigurableValueTypeId=1,Value="5",IsValid=true});Check((await leave.LoadAsync()).Values.Single().Value=="5","Tenant configuration save failed");
        permissions=false;await Denied(()=>leave.LoadAsync());permissions=true;
        var catalogue=new JobTypeConfigurationService(factory,access,logs);await Denied(()=>catalogue.SaveJobTypeAsync(new(){Name="Unauthorised global type"}));
        Check(!(await catalogue.SaveWorkflowAsync(new())).Success,"Retired workflow writer remains active");
        var groups=new GroupsManagementService(factory,access,logs);await groups.SaveGroupAsync(10,0,"Central group",1,1,0);var group=(await groups.LoadAsync()).Groups.Single();
        await Denied(()=>groups.LoadAsync(20));
        try{await groups.SaveGroupAsync(10,group.Id,"Stale",1,1,group.AuditId+1);throw new Exception("Stale group edit allowed");}catch(InvalidOperationException){}
        await using(var db=factory.CreateDbContext()){db.GroupStaffs.Add(new(){GroupId=group.Id,StaffIndividualId=200,EffectiveStartDate=DateTime.UtcNow.AddHours(5).Date});await db.SaveChangesAsync();}
        try{await groups.DeleteGroupAsync(10,group.Id,group.AuditId);throw new Exception("Group history deleted");}catch(InvalidOperationException){}
        var registry=new IndividualRegistrationService(factory,access,logs);actor.IsSuperAdministrator=true;
        var registration=new IndividualRegistrationModel{FirstName="New",LastName="Person",GenderTypeId=1,CountryId=1,DateOfBirth=new(1990,1,1),NationalId="A999001"};
        var person=await registry.RegisterAsync(registration);
        await using(var db=factory.CreateDbContext()){Check(await db.Individuals.AnyAsync(i=>i.BusinessEntityId==person)&&!await db.Users.AnyAsync(u=>u.BusinessEntityID==person)&&!await db.Jobs.AnyAsync(j=>j.IndividualID==person),"Registry registration created employment or account");Check((await db.BusinessEntities.SingleAsync(b=>b.BusinessEntityID==person)).VerifiedDate==null,"Manual identity was falsely verified");}
        try{await registry.RegisterAsync(registration);throw new Exception("Duplicate identity allowed");}catch(InvalidOperationException){}
        actor.IsSuperAdministrator=false;
        var generic=new RequestService(factory,access,null!);
        await using(var db=factory.CreateDbContext()){db.Requests.AddRange(new Request{RequestId=30,RequestTypeId=100,RequestStateId=2,ApplicantBusinessEntityId=200,OrganisationBusinessEntityID=10},new Request{RequestId=31,RequestTypeId=100,RequestStateId=2,ApplicantBusinessEntityId=100,OrganisationBusinessEntityID=20});await db.SaveChangesAsync();}
        Check(await generic.GetByIdAsync(31)==null,"Generic read crossed tenants");
        try{await generic.ChangeStateAsync(30,4);throw new Exception("Generic mutation bypassed workflow");}catch(InvalidOperationException){}
        var resolver=Proxy.For<IWorkflowApproverResolver>((_,_)=>Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>([new(1,1,1,true)]));
        var contexts=Proxy.For<IWorkflowRequestContextResolver>((_,_)=>Task.FromResult(new WorkflowRequestContextDto{RequestId=30,RequestTypeId=100,OrganisationBusinessEntityId=10,ApplicantBusinessEntityId=200,SubjectJobId=9,JobTypeId=1,RequestStateId=2}));
        await using(var db=factory.CreateDbContext())
        {
            db.RequestTypesAllowedRequestStateTransitions.Add(new(){RequestTypeId=100,FromRequestStateId=2,ToRequestStateId=4});
            db.WfWorkflowDefinitions.Add(new(){WorkflowDefinitionId=80,RequestTypeId=100,OrganisationBusinessEntityId=10,Name="Flow",IsActive=true,Stages=[new(){WorkflowStageId=81,StageOrder=1,StageName="HR",RequestStateId=2,ApprovalMode="Any"}]});
            db.WfRequestInstances.Add(new(){WorkflowRequestInstanceId=82,RequestId=30,WorkflowDefinitionId=80,CurrentRequestStageInstanceId=83});
            db.WfRequestStageInstances.Add(new(){RequestStageInstanceId=83,WorkflowRequestInstanceId=82,WorkflowStageId=81,Approvals=[new(){ApproverUserId=1,Decision="Pending"}]});
            db.StaffPositionChangeRequests.Add(new(){RequestId=30,OrganisationId=10,IndividualId=200,JobId=9,PositionId=1,ChangeKind="Assign",FromDate=DateTime.UtcNow.AddHours(5).Date.AddDays(-3),Reason="Backdated appointment",CreatedByUserId=1});await db.SaveChangesAsync();
        }
        var positions=new StaffPositionManagementService(factory,access,logs,null!,null!,resolver,contexts);
        Check((await positions.ActOnRequestAsync(30,2,"Approve","Reviewed")).Success,"Position approval failed");
        await using(var db=factory.CreateDbContext())
        {
            Check(await db.JobPositionsRequests.AnyAsync(l=>l.RequestId==30),"Position approval did not retain assignment linkage");
            var today=DateTime.UtcNow.AddHours(5).Date;var calendar=await AttendanceEligibilityCalendar.LoadAsync(db,200,today.AddDays(-2),today,10);
            Check(!calendar[today.AddDays(-2)].HasApprovedPosition&&calendar[today].HasApprovedPosition,"New position approval backdated attendance");
        }
        var otSettings=new JobTypeOvertimeSettingsService(factory,access,logs);actor.IsSuperAdministrator=true;
        await otSettings.SaveAsync(new(){OrganisationId=10,JobTypeId=1,IsAllowed=true,MaximumWeeklyHours=1,MaximumDailyHours=1});Check((await otSettings.LoadAsync(1)).MaximumWeeklyHours==1,"OT policy did not persist");
        var ot=new OvertimeRequestService(factory,access,null!,logs,null!,resolver,contexts);actor.IndividualId=200;
        Check(!(await ot.SubmitAsync(DateTime.UtcNow.AddHours(5).Date,120,"Above cap")).Success,"OT weekly cap bypassed");
        // A paid claim keeps its approved policy snapshot and cannot be approved or paid twice.
        var workDate=DateTime.UtcNow.AddHours(5).Date.AddDays(-1);
        await using(var db=factory.CreateDbContext())
        {
            db.RequestTypes.Add(new(){RequestTypeId=101,TypeName=OvertimeRequestService.TypeName});
            db.RequestTypesAllowedRequestStateTransitions.Add(new(){RequestTypeId=101,FromRequestStateId=2,ToRequestStateId=4});
            db.WorkTemplateTypes.Add(new(){WorkTemplateTypeId=90,Name="Official",Code="OFFICIAL"});
            db.WorkPlans.Add(new(){WorkPlanId=90,JobId=9,IndividualId=200,OrganisationBusinessEntityId=10,WorkDate=workDate,IsValid=true});
            db.WorkAssignments.Add(new(){WorkAssignmentId=90,WorkPlanId=90,WorkTemplateTypeId=90,Name="Official",SchedulePurpose="Official",IsValid=true,StartDateTime=workDate.AddHours(8),EndDateTime=workDate.AddHours(14)});
            db.AttendanceLogs.AddRange(new AttendanceLog{AttendanceLogId=90,IndividualId=200,OrganisationId=10,AttendanceLogStateId=1,Date=workDate.AddHours(8),AttendanceLogResolutions=[new(){IsValid=true,AttendanceResolutionStatusId=HRM.Constants.AttendanceResolutionStatusIds.Resolved,WorkAssignmentId=90,AttendanceClockTypeId=1}]},
                new AttendanceLog{AttendanceLogId=91,IndividualId=200,OrganisationId=10,AttendanceLogStateId=1,Date=workDate.AddHours(15),AttendanceLogResolutions=[new(){IsValid=true,AttendanceResolutionStatusId=HRM.Constants.AttendanceResolutionStatusIds.Resolved,WorkAssignmentId=90,AttendanceClockTypeId=2}]});
            db.AttendanceLogs.Add(new AttendanceLog{AttendanceLogId=92,IndividualId=200,OrganisationId=10,AttendanceLogStateId=1,Date=workDate.AddHours(14)});
            await db.SaveChangesAsync();
        }
        var create=Proxy.For<IRequestService>((m,a)=>m=="CreateAsync"?CreateOt((HrmTeContext)a[0]!,(HRM.DTOs.CreateRequestDto)a[1]!):throw new Exception(m));
        var start=Proxy.For<IWorkflowEngine>((m,a)=>m=="StartAsync"?StartOt((HrmTeContext)a[0]!,Convert.ToInt32(a[1])):throw new Exception(m));
        ot=new OvertimeRequestService(factory,access,create,logs,start,resolver,contexts);
        var submitted=await ot.SubmitClaimAsync(92,91,"Completed extra hours");Check(submitted.Success,"Completed OT could not be submitted: "+submitted.Message);
        Check(!(await ot.SubmitClaimAsync(92,91,"Over cumulative daily/weekly cap")).Success,"Pending OT did not consume allowance");
        int claim;await using(var db=factory.CreateDbContext())claim=await db.OvertimeRequests.Select(r=>r.RequestId).SingleAsync();
        await Denied(()=>ot.ActAsync(claim,2,"Approve","Self approval"));
        actor.IndividualId=100;
        Check((await ot.ActAsync(claim,2,"Approve","Checked attendance")).Success,"Assigned OT approval failed");
        Check(!(await ot.ActAsync(claim,2,"Approve","Repeat")).Success,"Completed OT was approved twice");
        Check((await ot.RecordPaymentAsync(claim,"PAYROLL-TEST")).Success,"Approved OT payment reference not recorded");
        Check(!(await ot.RecordPaymentAsync(claim,"REPEAT")).Success,"OT payment recorded twice");
        await using(var db=factory.CreateDbContext()){var row=await db.OvertimeRequests.SingleAsync();Check(row.ApprovedPolicyJson!=null&&row.PaymentReference=="PAYROLL-TEST"&&row.PaidAtUtc.HasValue,"OT audit snapshot/payment missing");}
        var scope=new ScopeScheduleSettingsService(factory,access,logs);actor.IsSuperAdministrator=false;actor.IndividualId=100;
        await Denied(()=>scope.LoadAsync(20));
        await using(var db=factory.CreateDbContext())
        {
            var day=DateTime.UtcNow.AddHours(5).Date;
            db.WorkTemplates.AddRange(new HRM.Models.WorkPlanning.WorkTemplate{WorkTemplateId=91,Name="Organisation default",IsActive=true,OrganisationBusinessEntityId=10},
                new HRM.Models.WorkPlanning.WorkTemplate{WorkTemplateId=92,Name="Group override",IsActive=true,OrganisationBusinessEntityId=10});
            db.ScopeScheduleAssignments.AddRange(new(){Id=91,OrganisationId=10,WorkTemplateId=91,EffectiveFrom=day,SchedulePurpose="Official"},
                new(){Id=92,OrganisationId=10,GroupId=group.Id,WorkTemplateId=92,EffectiveFrom=day,SchedulePurpose="Official"});
            await db.SaveChangesAsync();var job=await db.Jobs.SingleAsync(j=>j.JobId==9);
            var effective=EffectiveStaffSchedules.On(await EffectiveStaffSchedules.LoadAsync(db,job,day,day),day);
            Check(effective.Count==1&&effective[0].Assignment.WorkTemplateID==92,"Group schedule did not override organisation default");
            db.JobWorkTemplateAssignments.Add(new(){JobID=9,WorkTemplateID=91,EffectiveFrom=day,IsActive=true,SchedulePurpose="Official"});await db.SaveChangesAsync();
            effective=EffectiveStaffSchedules.On(await EffectiveStaffSchedules.LoadAsync(db,job,day,day),day);
            Check(effective.Count==1&&effective[0].Assignment.WorkTemplateID==91,"Staff override did not win shared-schedule precedence");
        }
        Console.WriteLine("PASS Gap hardening: configuration permissions/tenants, canonical writers, registry identity duplicates, group history/concurrency, position approval dates, generic requests, OT caps and shared-schedule tenant denial");
    }
    static async Task<Request> CreateOt(HrmTeContext db,HRM.DTOs.CreateRequestDto dto)
    {
        var request=new Request{RequestTypeId=dto.RequestTypeId,RequestStateId=2,ApplicantBusinessEntityId=200,OrganisationBusinessEntityID=10};
        db.Requests.Add(request);await db.SaveChangesAsync();return request;
    }
    static async Task<long> StartOt(HrmTeContext db,int request)
    {
        db.WfWorkflowDefinitions.Add(new(){WorkflowDefinitionId=90,RequestTypeId=101,OrganisationBusinessEntityId=10,Name="OT approval",Stages=[new(){WorkflowStageId=91,StageOrder=1,StageName="HR",RequestStateId=2,ApprovalMode="Any"}]});
        db.WfRequestInstances.Add(new(){WorkflowRequestInstanceId=92,RequestId=request,WorkflowDefinitionId=90,CurrentRequestStageInstanceId=93});
        db.WfRequestStageInstances.Add(new(){RequestStageInstanceId=93,WorkflowRequestInstanceId=92,WorkflowStageId=91,Approvals=[new(){ApproverUserId=1,Decision="Pending"}]});
        await db.SaveChangesAsync();return 92;
    }
    static async Task<OperationLog> Audit(HrmTeContext db){var audit=new OperationLog();db.OperationLogs.Add(audit);await db.SaveChangesAsync();return audit;}
}
