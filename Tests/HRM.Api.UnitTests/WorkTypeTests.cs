using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace HRM.Api.UnitTests;

public class WorkTypeTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Assignment_create_and_update_persist_selected_active_state(bool active)
    {
        var factory=new QueryFactory(ignoreTransactions:true);
        var access=new FakeAccess();access.Permissions.Add("WorkTypes.Manage");
        var today=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
        await using(var db=factory.CreateDbContext()) {
            db.WorkTypes.Add(new WorkType {WorkTypeId=1,Name="Office",IsValid=true});
            db.Jobs.Add(new Job {JobId=1,IndividualID=100,OrganisationID=10,JobStateId=4,JoinedDate=today.AddDays(-10).ToDateTime(TimeOnly.MinValue)});
            await db.SaveChangesAsync();
        }
        var service=new WorkTypeService(factory,access,NullLogger<WorkTypeService>.Instance);
        Assert.True((await service.AssignAsync(new(){JobId=1,WorkTypeId=1,FromDate=today,IsValid=active})).Success);
        int id;
        await using(var db=factory.CreateDbContext()) {
            var row=Assert.Single(db.AssignedWorkTypes);id=row.AssignedWorkTypeId;Assert.Equal(active,row.IsValid);
        }
        Assert.True((await service.UpdateAssignmentAsync(new(){AssignedWorkTypeId=id,WorkTypeId=1,FromDate=today,IsValid=!active})).Success);
        await using(var db=factory.CreateDbContext()) Assert.Equal(!active,Assert.Single(db.AssignedWorkTypes).IsValid);
    }
    [Theory]
    [InlineData(true,true,0,0,true)]
    [InlineData(false,true,0,0,false)]
    [InlineData(true,false,0,0,false)]
    [InlineData(true,true,1,2,false)]
    [InlineData(true,true,-2,-1,false)]
    [InlineData(true,true,-2,0,true)]
    public async Task Clock_requires_an_active_type_and_assignment_covering_today(bool typeActive,bool assignmentActive,int start,int end,bool expected)
    {
        var factory=new QueryFactory(); await using var db=factory.CreateDbContext();
        var day=new DateOnly(2026,10,29);
        db.WorkTypes.Add(new WorkType {WorkTypeId=1,Name="Office",IsValid=typeActive});
        db.Jobs.Add(new Job {JobId=1,IndividualID=100,OrganisationID=10,JobStateId=4,JoinedDate=day.AddDays(-10).ToDateTime(TimeOnly.MinValue)});
        db.AssignedWorkTypes.Add(new AssignedWorkType {AssignedWorkTypeId=1,WorkTypeId=1,JobId=1,OrganisationId=10,IsValid=assignmentActive,FromDate=day.AddDays(start),ToDate=day.AddDays(end)});
        await db.SaveChangesAsync();
        Assert.Equal(expected,await WorkTypeService.HasClockAssignmentAsync(db,100,1,10,day));
        Assert.False(await WorkTypeService.HasClockAssignmentAsync(db,100,1,20,day));
        Assert.False(await WorkTypeService.HasClockAssignmentAsync(db,200,1,10,day));
    }
    [Fact]
    public async Task Catalogue_and_assignments_reject_unprivileged_users()
    {
        var service=new WorkTypeService(new QueryFactory(),new FakeAccess(),NullLogger<WorkTypeService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.GetAllAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.CreateAsync(new(){Name="Office"}));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.AssignAsync(new(){WorkTypeId=1,JobId=1,FromDate=new(2026,10,29)}));
    }
    [Fact]
    public async Task Work_type_permission_does_not_allow_cross_tenant_access_or_catalogue_writes()
    {
        var access=new FakeAccess();access.Permissions.Add("WorkTypes.Manage");
        var service=new WorkTypeService(new QueryFactory(),access,NullLogger<WorkTypeService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.GetAssignmentsAsync(organisationId:20));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.CreateAsync(new(){Name="Office"}));
    }
}
