using HRM.DTOs;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class StaffReappointmentTests
{
    [Theory]
    [InlineData("Approve")]
    [InlineData("Reject")]
    [InlineData("Cancel")]
    public async Task Former_staff_appointments_follow_approvals_without_changing_old_employment(string action)
    {
        var factory=new QueryFactory(ignoreTransactions:true);var access=new FakeAccess();access.Actor!.IsHRStaff=true;
        var today=FormerStaffPolicy.Today;
        await using(var db=factory.CreateDbContext())
        {
            db.Individuals.Add(new(){BusinessEntityId=820,FirstNameEnglish="Ahmed",LastNameEnglish="Farish"});
            db.Idcards.Add(new(){BusinessEntityId=820,IdcardNumber="A747888"});
            db.Organisations.Add(new(){BusinessEntityID=10,OrganisationName="Tenant"});
            db.UserOrganisations.Add(new(){UserOrganisationID=1,BusinessEntityID=10});
            db.Users.AddRange(new(){UserId=1,BusinessEntityID=100,Username="HR",UserOrganisationId=1,IsActive=true,LocalUserStateId=1},new(){UserId=2,BusinessEntityID=200,Username="Approver",UserOrganisationId=1,IsActive=true,LocalUserStateId=1});
            db.Jobs.Add(new(){JobId=918,IndividualID=820,OrganisationID=10,JobStateId=8,JoinedDate=today.AddYears(-1),TerminatedDate=today.AddDays(-1)});
            db.JobTypes.Add(new(){JobTypeId=1,TypeName="Contract",IsActive=true});
            db.OrganisationStructureTypes.Add(new(){OrganisationStructureTypeId=1,TypeName="Department"});
            db.OrganisationStructures.Add(new(){OrganisationStructureId=1,OrganisationBusinessEntityID=10,OrganisationStructureTypeId=1,OrganisationStructureStateId=3,Name="Department"});
            db.RequestTypes.Add(new(){RequestTypeId=32,TypeName=StaffOnboardingService.RequestTypeName});
            foreach(var id in new[]{1,2,4,5,6})db.RequestStates.Add(new(){RequestStateId=id,StateName="State "+id,IsProcessingState=id<4,IsFinalState=id>=4});
            foreach(var (from,to) in new[]{(1,2),(2,4),(2,5),(2,6)})db.RequestTypesAllowedRequestStateTransitions.Add(new(){RequestTypeId=32,FromRequestStateId=from,ToRequestStateId=to});
            db.UserGroups.Add(new(){UserGroupId=1,GroupName="HR approvers",UserOrganisationId=1});
            db.UserAssignedUserGroups.Add(new(){UserGroupId=1,UserId=2});
            db.WfWorkflowDefinitions.Add(new(){WorkflowDefinitionId=1,RequestTypeId=32,OrganisationBusinessEntityId=10,Name="Appointment",IsActive=true,
                WorkflowJobTypes=[new(){JobTypeId=1,IsActive=true}],
                Stages=[new(){WorkflowStageId=1,StageOrder=1,StageName="HR approval",RequestStateId=2,ApprovalMode="Any",Approvers=[new(){StageApproverId=1,ApproverType="UserGroup",ApproverReferenceId=1,IsRequired=true}]}]});
            await db.SaveChangesAsync();
        }
        var logs=StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=1}));
        var service=new StaffOnboardingService(factory,access,logs,new WorkflowApproverResolver(factory));
        Assert.Equal("Ahmed Farish",(await service.LoadAsync(820)).Individual);
        var submitted=await service.SubmitAsync(new StaffOnboardingForm{IndividualId=820,OrganisationStructureId=1,JobTypeId=1,JoinedDate=today});
        Assert.True(submitted.Success,submitted.Message);
        await using(var db=factory.CreateDbContext())
        {
            Assert.Single(await db.Jobs.Where(x=>x.IndividualID==820).ToListAsync());
            Assert.False(await db.Users.AnyAsync(x=>x.BusinessEntityID==820));
        }
        var row=Assert.Single(await service.GetRequestsAsync());
        if(action!="Cancel"){access.Actor.UserId=2;access.Actor.IndividualId=200;}
        var result=await service.ActAsync(row.RequestId,2,action,"Appointment decision");
        Assert.True(result.Success,result.Message);
        await using(var db=factory.CreateDbContext())
        {
            var old=await db.Jobs.SingleAsync(x=>x.JobId==918);
            Assert.Equal(8,old.JobStateId);Assert.Equal(today.AddDays(-1),old.TerminatedDate);
            Assert.Equal(action=="Approve"?2:1,await db.Jobs.CountAsync(x=>x.IndividualID==820));
            Assert.Equal(action=="Approve",await db.Users.AnyAsync(x=>x.BusinessEntityID==820));
            old.TerminatedDate=today;
            var denied=await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());
            Assert.Equal(FormerStaffPolicy.ReadOnlyMessage,denied.Message);
        }
    }
}
