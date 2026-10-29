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
}
