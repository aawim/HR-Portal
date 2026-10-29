using HRM.Application.Contracts.Pages;
using HRM.Components.Shared;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Attendance.Abstraction;
using HRM.Services.Attendance.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class AttendanceRequestReviewTests
{
    private sealed class Fixture
    {
        public QueryFactory Factory {get;}=new(ignoreTransactions:true);
        public FakeAccess Access {get;}=new();
        public AttendanceRequestReviewService Service {get;}
        public int Processed {get;private set;}
        public Fixture()
        {
            Service=new(Factory,Access,
                StubProxy.For<IWorkflowApproverResolver>((_,args)=>Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>([new((int)args[0]!,1,1,true)])),
                StubProxy.For<IWorkflowRequestContextResolver>((_,_)=>Task.FromResult(new WorkflowRequestContextDto())),
                StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=1})),
                StubProxy.For<IAttendanceLogProcessor>((_,args)=>{Processed++;return Task.FromResult(new AttendanceProcessingResult{Success=true,AttendanceLogId=(int)args[0]!});}),
                NullLogger<AttendanceRequestReviewService>.Instance);
        }
        public async Task Seed(bool correction=false,bool twoStages=true)
        {
            await using var db=Factory.CreateDbContext();var day=DateTime.UtcNow.AddHours(5).Date.AddDays(-1).AddHours(8);
            db.Individuals.AddRange(new(){BusinessEntityId=100,FirstNameEnglish="Supervisor"},new(){BusinessEntityId=200,FirstNameEnglish="Applicant"},new(){BusinessEntityId=300,FirstNameEnglish="HR"});
            db.Organisations.Add(new(){BusinessEntityID=10,OrganisationName="Tenant"});
            var organisation=new UserOrganisation{UserOrganisationID=1,BusinessEntityID=10};
            db.Users.AddRange(new(){UserId=1,BusinessEntityID=100,Username="supervisor",IsActive=true,LocalUserStateId=1,UserOrganisation=organisation},new(){UserId=2,BusinessEntityID=300,Username="hr",IsActive=true,LocalUserStateId=1,UserOrganisation=organisation});
            db.Jobs.Add(new(){JobId=10,OrganisationID=10,IndividualID=200,JobStateId=4,JoinedDate=day.AddYears(-1)});
            db.RequestStates.AddRange(new(){RequestStateId=2,StateName="Pending Verification",IsProcessingState=true},new(){RequestStateId=7,StateName="Pending HR Approval",IsProcessingState=true},new(){RequestStateId=4,StateName="Approved",IsFinalState=true},new(){RequestStateId=6,StateName="Rejected",IsFinalState=true});
            var type=correction?SharedConfig.RequestTypes.ATTENDANCE_LOG_CHANGE_REQUEST:SharedConfig.RequestTypes.ATTENDANCE_LOG_REQUEST;
            db.RequestTypes.Add(new(){RequestTypeId=type,TypeName="Attendance request"});
            var request=new Request{RequestId=1,ApplicantBusinessEntityId=200,OrganisationBusinessEntityID=10,RequestTypeId=type,RequestStateId=2};
            var proposed=new AttendanceLog{AttendanceLogId=20,IndividualId=200,OrganisationId=10,Date=day,InOutModeId=1,AttendanceLogStateId=SharedConfig.AttendanceLogStates.Invalid};
            db.AttendanceLogs.Add(proposed);
            if(correction)
            {
                db.AttendanceLogs.Add(new(){AttendanceLogId=10,IndividualId=200,OrganisationId=10,Date=day.AddMinutes(10),AttendanceLogStateId=SharedConfig.AttendanceLogStates.Valid,InOutModeId=1});proposed.RelatedAttendanceLogId=10;
                db.AttendanceLogResolutions.Add(new(){AttendanceLogResolutionId=1,AttendanceLogId=10,IsValid=true});
                db.AttendanceLogChangeRequests.Add(new(){RequestId=1,Request=request,AttendanceLogId=20});
            }
            else db.AttendanceLogRequests.Add(new(){RequestId=1,Request=request,AttendanceLogId=20});
            var definition=new WfWorkflowDefinition{WorkflowDefinitionId=1,RequestTypeId=type};
            var verify=new WfWorkflowStage{WorkflowStageId=1,WorkflowDefinition=definition,StageOrder=1,StageName="Supervisor verification",RequestStateId=2};definition.Stages.Add(verify);
            if(twoStages)definition.Stages.Add(new(){WorkflowStageId=2,WorkflowDefinition=definition,StageOrder=2,StageName="HR approval",RequestStateId=7});
            var instance=new WfRequestInstance{WorkflowRequestInstanceId=1,RequestId=1,WorkflowDefinition=definition};
            var stage=new WfRequestStageInstance{RequestStageInstanceId=1,WorkflowRequestInstance=instance,WorkflowStage=verify};stage.Approvals.Add(new(){ApproverUserId=1});instance.CurrentStage=stage;db.WfRequestInstances.Add(instance);
            db.RequestTypesAllowedRequestStateTransitions.AddRange(new(){RequestTypeId=type,FromRequestStateId=2,ToRequestStateId=twoStages?7:4},new(){RequestTypeId=type,FromRequestStateId=7,ToRequestStateId=4},new(){RequestTypeId=type,FromRequestStateId=2,ToRequestStateId=6});
            await db.SaveChangesAsync();
        }
    }
    [Fact]
    public async Task Verification_then_approval_uses_assigned_stages_and_only_final_approval_activates_log()
    {
        var f=new Fixture();await f.Seed();var review=await f.Service.GetAsync(1);Assert.True(review.CanApprove);Assert.Equal("Verify stage",review.ActionLabel);
        Assert.True((await f.Service.ActAsync(1,2,"Approve","Verified")).Success);
        await using(var db=f.Factory.CreateDbContext()){Assert.Equal(7,(await db.Requests.SingleAsync()).RequestStateId);Assert.Equal(SharedConfig.AttendanceLogStates.Invalid,(await db.AttendanceLogs.SingleAsync()).AttendanceLogStateId);}Assert.Equal(0,f.Processed);
        Assert.False((await f.Service.GetAsync(1)).CanApprove);
        f.Access.Actor!.UserId=2;f.Access.Actor.IndividualId=300;review=await f.Service.GetAsync(1);Assert.True(review.CanApprove);Assert.Equal("Approve stage",review.ActionLabel);
        Assert.True((await f.Service.ActAsync(1,7,"Approve","Approved")).Success);Assert.Equal(1,f.Processed);
        await using(var db=f.Factory.CreateDbContext()){Assert.Equal(4,(await db.Requests.SingleAsync()).RequestStateId);Assert.Equal(SharedConfig.AttendanceLogStates.Valid,(await db.AttendanceLogs.SingleAsync()).AttendanceLogStateId);}
        Assert.False((await f.Service.GetAsync(1)).CanApprove);
    }
    [Fact]
    public async Task Self_approval_unassigned_reviewers_and_other_tenants_are_denied()
    {
        var f=new Fixture();await f.Seed();f.Access.Actor!.IndividualId=200;
        Assert.False((await f.Service.GetAsync(1)).CanApprove);await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.ActAsync(1,2,"Approve","Self"));
        f.Access.Actor.IndividualId=999;f.Access.Actor.UserId=999;await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.GetAsync(1));
        f.Access.Organisation=20;await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.GetAsync(1));
    }
    [Fact]
    public async Task Final_correction_invalidates_original_clock_and_resolution_only_after_approval()
    {
        var f=new Fixture();await f.Seed(correction:true,twoStages:false);
        Assert.True((await f.Service.ActAsync(1,2,"Approve","Correction verified")).Success);
        await using var db=f.Factory.CreateDbContext();Assert.Equal(SharedConfig.AttendanceLogStates.Invalid,(await db.AttendanceLogs.SingleAsync(x=>x.AttendanceLogId==10)).AttendanceLogStateId);Assert.False((await db.AttendanceLogResolutions.SingleAsync()).IsValid);Assert.Equal(SharedConfig.AttendanceLogStates.Valid,(await db.AttendanceLogs.SingleAsync(x=>x.AttendanceLogId==20)).AttendanceLogStateId);Assert.Equal(1,f.Processed);
    }
    [Fact]
    public async Task Reject_and_stale_actions_do_not_activate_clock_events()
    {
        var f=new Fixture();await f.Seed();Assert.False((await f.Service.ActAsync(1,7,"Approve","Stale state")).Success);
        Assert.True((await f.Service.ActAsync(1,2,"Reject","Incorrect time")).Success);
        await using var db=f.Factory.CreateDbContext();Assert.Equal(6,(await db.Requests.SingleAsync()).RequestStateId);Assert.Equal(SharedConfig.AttendanceLogStates.Invalid,(await db.AttendanceLogs.SingleAsync()).AttendanceLogStateId);Assert.Equal(0,f.Processed);
    }
}
