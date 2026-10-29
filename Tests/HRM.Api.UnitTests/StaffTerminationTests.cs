using System.Text.Json;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Assignment=HRM.Services.Contracts.StaffPositionManagementServiceContracts.Assignment;

namespace HRM.Api.UnitTests;

public sealed class StaffTerminationTests
{
    private readonly QueryFactory factory=new(ignoreTransactions:true);
    private readonly FakeAccess access=new();
    private int workflowStarts;
    private static DateTime Today=>DateTime.UtcNow.AddHours(5).Date;
    private StaffPositionManagementService Service=>new(factory,access,
        StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=9})),
        StubProxy.For<IRequestService>((_,args)=>{var dto=(CreateRequestDto)args[1]!;return Task.FromResult(new Request{RequestId=100,RequestTypeId=dto.RequestTypeId,RequestStateId=1,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=50});}),
        StubProxy.For<IWorkflowEngine>((_,_)=>{workflowStarts++;return Task.FromResult(1L);}),null!,null!);
    private async Task<Assignment> Seed()
    {
        access.Actor!.IsSuperAdministrator=true;await using var db=factory.CreateDbContext();
        db.RequestStates.Add(new(){RequestStateId=1,StateName="Incomplete"});
        db.RequestTypes.AddRange(new(){RequestTypeId=22,TypeName="Job Resignation Request"},new(){RequestTypeId=30,TypeName="Annual Leave"});
        db.Positions.Add(new(){PositionId=1,OrganisationId=10,Name="Officer",IsActive=true});
        db.Jobs.Add(new(){JobId=5,IndividualID=50,OrganisationID=10,JobStateId=4,JoinedDate=Today.AddMonths(-2)});
        var assignment=new JobPosition{JobPositionId=1,JobId=5,PositionId=1,FromDate=Today.AddMonths(-1),JobPositionStateId=4,OperationLogId=1};db.JobPositions.Add(assignment);await db.SaveChangesAsync();
        return new(1,5,1,"Officer",assignment.FromDate,null,4,"Approved",1);
    }
    [Fact]public async Task Submission_uses_selected_job_request_type_and_keeps_employment_unchanged()
    {
        var original=await Seed();await Service.SaveAsync(50,5,1,original.From,Today,"Resigning",original,remove:true,terminationRequestTypeId:22);
        await using var db=factory.CreateDbContext();var r=await db.StaffPositionChangeRequests.Include(x=>x.Request).SingleAsync();
        Assert.Equal(22,r.Request.RequestTypeId);Assert.Equal(1,workflowStarts);Assert.Null((await db.Jobs.SingleAsync()).TerminatedDate);Assert.Null((await db.JobPositions.SingleAsync()).ToDate);Assert.Single(db.JobRequests);
    }
    [Fact]public async Task Load_includes_termination_dropdown_options_in_the_http_response_contract()
    {
        await Seed();await using(var db=factory.CreateDbContext())
        {
            // No active employment choices are needed for this contract regression.
            (await db.Jobs.SingleAsync()).JobStateId=7;db.JobPositionStates.Add(new(){JobPositionStateId=4,StateName="Approved"});await db.SaveChangesAsync();
        }
        var data=await Service.LoadAsync(50);var option=Assert.Single(data.TerminationRequestTypes);Assert.Equal(22,option.Id);Assert.Equal("Job Resignation Request",option.Name);
        var jsonOptions=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json=JsonSerializer.Serialize(data,jsonOptions);
        var received=JsonSerializer.Deserialize<HRM.Services.Contracts.StaffPositionManagementServiceContracts.Data>(json,jsonOptions)!;
        Assert.Equal(option,Assert.Single(received.TerminationRequestTypes));Assert.DoesNotContain(received.TerminationRequestTypes,x=>x.Id==30);
    }
    [Theory][InlineData(null)][InlineData(30)]public async Task Missing_or_unrelated_termination_type_is_rejected(int? type)
    {
        var original=await Seed();await Assert.ThrowsAsync<InvalidOperationException>(()=>Service.SaveAsync(50,5,1,original.From,Today,"Leaving",original,remove:true,terminationRequestTypeId:type));
        await using var db=factory.CreateDbContext();Assert.Empty(db.StaffPositionChangeRequests);Assert.Equal(0,workflowStarts);Assert.Null((await db.Jobs.SingleAsync()).TerminatedDate);
    }
    [Theory][InlineData(0)][InlineData(7)]public async Task Only_final_approval_ends_job_and_assignments_and_releases_position_on_effective_date(int offset)
    {
        var original=await Seed();var end=Today.AddDays(offset);
        await Service.SaveAsync(50,5,1,original.From,end,"Resigning",original,remove:true,terminationRequestTypeId:22);
        await using var db=factory.CreateDbContext();db.FrameworkJobStepHistories.Add(new(){JobId=5,PositionMatrixId=1,JobMatrixStepId=1,EffectiveFrom=original.From,BasicSalarySnapshot=1000});await db.SaveChangesAsync();var r=await db.StaffPositionChangeRequests.Include(x=>x.Request).SingleAsync();
        await Service.ProcessRequestedChangeAsync(db,r,false);Assert.Null((await db.Jobs.SingleAsync()).TerminatedDate);
        await Service.ProcessRequestedChangeAsync(db,r,true);
        var job=await db.Jobs.SingleAsync();Assert.Equal(end,job.TerminatedDate);Assert.Equal(offset==0?7:4,job.JobStateId);Assert.Equal(end,(await db.JobPositions.SingleAsync()).ToDate);
        Assert.Equal(end,(await db.FrameworkJobStepHistories.SingleAsync()).EffectiveTo);
        Assert.Single(await PositionVacancy.Vacant(db,end).ToListAsync());
        if(offset>0)Assert.Empty(await PositionVacancy.Vacant(db,Today).ToListAsync());
        Assert.Equal(1,await db.JobPositions.CountAsync());
    }
    [Fact]public async Task Changed_position_version_blocks_termination_execution()
    {
        var original=await Seed();await Service.SaveAsync(50,5,1,original.From,Today,"Leaving",original,remove:true,terminationRequestTypeId:22);
        await using var db=factory.CreateDbContext();(await db.JobPositions.SingleAsync()).OperationLogId=10;await db.SaveChangesAsync();
        var r=await db.StaffPositionChangeRequests.Include(x=>x.Request).SingleAsync();await Assert.ThrowsAsync<InvalidOperationException>(()=>Service.ProcessRequestedChangeAsync(db,r,true));Assert.Null((await db.Jobs.SingleAsync()).TerminatedDate);
    }
    [Fact]public async Task An_active_position_blocks_an_additional_assignment_even_to_a_different_job_or_future_period()
    {
        await Seed();await using(var db=factory.CreateDbContext())
        {
            db.Positions.Add(new(){PositionId=2,OrganisationId=10,Name="Second position",IsActive=true});
            db.Jobs.Add(new(){JobId=6,IndividualID=50,OrganisationID=10,JobStateId=4,JoinedDate=Today});await db.SaveChangesAsync();
        }
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>Service.SaveAsync(50,6,2,Today.AddDays(30),null,"Another assignment"));
        Assert.Contains("already has an active position",error.Message);await using var check=factory.CreateDbContext();Assert.Single(check.JobPositions);Assert.Empty(check.StaffPositionChangeRequests);Assert.Equal(0,workflowStarts);
    }
    [Fact]public async Task A_new_approved_job_can_receive_a_position_after_the_previous_employment_has_ended()
    {
        await Seed();await PrepareNewJob();
        await Service.SaveAsync(50,6,2,Today,null,"New employment");await using var db=factory.CreateDbContext();var r=await db.StaffPositionChangeRequests.Include(x=>x.Request).SingleAsync();
        await Service.ProcessRequestedChangeAsync(db,r,true);Assert.Equal(2,await db.JobPositions.CountAsync());Assert.Equal(6,(await db.JobPositions.SingleAsync(x=>x.PositionId==2)).JobId);
    }
    [Fact]public async Task Approval_rechecks_that_another_position_has_not_become_active_since_submission()
    {
        await Seed();await PrepareNewJob();await Service.SaveAsync(50,6,2,Today,null,"New employment");
        await using var db=factory.CreateDbContext();db.Positions.Add(new(){PositionId=3,OrganisationId=10,Name="Other assignment",IsActive=true});
        db.JobPositions.Add(new(){JobPositionId=3,JobId=6,PositionId=3,FromDate=Today,JobPositionStateId=4});await db.SaveChangesAsync();
        var r=await db.StaffPositionChangeRequests.Include(x=>x.Request).SingleAsync();var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>Service.ProcessRequestedChangeAsync(db,r,true));Assert.Contains("already has an active position",error.Message);Assert.False(await db.JobPositions.AnyAsync(x=>x.PositionId==2));
    }
    [Fact]public async Task Editing_dates_cannot_expand_an_assignment_into_another_position_period()
    {
        var original=await Seed();await using(var db=factory.CreateDbContext())
        {
            db.Positions.Add(new(){PositionId=2,OrganisationId=10,Name="Previous position",IsActive=true});
            db.JobPositions.Add(new(){JobId=5,PositionId=2,FromDate=Today.AddMonths(-2),ToDate=original.From,JobPositionStateId=4});await db.SaveChangesAsync();
        }
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>Service.SaveAsync(50,5,1,original.From.AddDays(-1),null,"Extend dates",original));Assert.Contains("already has a position assigned",error.Message);
    }
    private async Task PrepareNewJob()
    {
        await using var db=factory.CreateDbContext();var old=await db.Jobs.SingleAsync();old.TerminatedDate=Today;old.JobStateId=7;(await db.JobPositions.SingleAsync()).ToDate=Today;
        db.Jobs.Add(new(){JobId=6,IndividualID=50,OrganisationID=10,JobStateId=4,JoinedDate=Today});db.Positions.Add(new(){PositionId=2,OrganisationId=10,Name="New position",IsActive=true});
        db.RequestTypes.Add(new(){RequestTypeId=31,TypeName="Staff Position Assignment Request"});await db.SaveChangesAsync();
    }
    [Fact]public async Task Terminating_a_legacy_shared_position_does_not_require_the_other_employment_to_vacate_it()
    {
        var original=await Seed();await using(var db=factory.CreateDbContext())
        {
            db.Jobs.Add(new(){JobId=6,IndividualID=60,OrganisationID=10,JobStateId=4,JoinedDate=Today.AddMonths(-2)});
            db.JobPositions.Add(new(){JobPositionId=2,JobId=6,PositionId=1,FromDate=Today.AddMonths(-1),JobPositionStateId=4});await db.SaveChangesAsync();
        }
        await Service.SaveAsync(50,5,1,original.From,Today,"Resigning",original,remove:true,terminationRequestTypeId:22);
        await using var check=factory.CreateDbContext();var request=await check.StaffPositionChangeRequests.Include(x=>x.Request).SingleAsync();
        Assert.Null((await check.Jobs.SingleAsync(x=>x.JobId==5)).TerminatedDate);
        await Service.ProcessRequestedChangeAsync(check,request,true);
        Assert.Equal(Today,(await check.Jobs.SingleAsync(x=>x.JobId==5)).TerminatedDate);
        Assert.Null((await check.Jobs.SingleAsync(x=>x.JobId==6)).TerminatedDate);Assert.Null((await check.JobPositions.SingleAsync(x=>x.JobId==6)).ToDate);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PositionVacancy.RequireAsync(check,1,Today,null));
    }
}
