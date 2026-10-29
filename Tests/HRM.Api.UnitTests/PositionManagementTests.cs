using HRM.Application.Contracts.Pages;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Api.UnitTests;
public sealed class PositionManagementTests
{
    private readonly QueryFactory factory=new(ignoreTransactions:true);
    private readonly FakeAccess access=new();
    private PositionManagementService Service=>new(factory,access,StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=1})));
    private async Task Seed()
    {
        access.Permissions.Add("Positions.Configure");await using var db=factory.CreateDbContext();
        db.PositionFrameworks.AddRange(new(){FrameworkId=1,Code="LEGACY",Name="Custom",IsActive=true},new(){FrameworkId=2,Code="NEW_PAY",Name="Government",IsActive=true});
        db.Positions.AddRange(new(){PositionId=1,OrganisationId=10,Name="Existing",PositionFrameworkId=1,IsActive=true},new(){PositionId=2,OrganisationId=20,Name="Other tenant",PositionFrameworkId=1,IsActive=true});
        db.PositionMatrices.AddRange(new(){PositionMatrixId=1,OrganisationId=10,JobMatrixId=1,JobTitleId=1,IsActive=true,EffectiveFrom=DateTime.UtcNow.AddDays(-10)},new(){PositionMatrixId=2,OrganisationId=20,JobMatrixId=1,JobTitleId=1,IsActive=true,EffectiveFrom=DateTime.UtcNow.AddDays(-10)});
        db.FrameworkJobMatrices.Add(new(){JobMatrixId=1,MatrixCode="G1",Name="Government grade",BasicSalary=5000,IsActive=true});db.FrameworkJobTitles.Add(new(){JobTitleId=1,Name="Officer",IsActive=true});await db.SaveChangesAsync();
    }
    [Fact]public async Task Shared_positions_table_supports_both_frameworks_and_blocks_cross_tenant_matrices()
    {
        await Seed();Assert.True((await Service.SaveAsync(new(0,"Custom officer",null,null,true,false,1,null))).Success);
        Assert.True((await Service.SaveAsync(new(0,"Government officer",null,null,true,false,2,1))).Success);
        Assert.False((await Service.SaveAsync(new(0,"Foreign matrix",null,null,true,false,2,2))).Success);
        var data=await Service.LoadAsync();Assert.DoesNotContain(data.Positions,x=>x.Name=="Other tenant");Assert.Single(data.Matrices);Assert.Contains(data.Positions,x=>x.FrameworkId==2&&x.MatrixId==1);
    }
    [Fact]public async Task Position_configuration_requires_permission_and_cannot_edit_another_tenant()
    {
        await Seed();await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service.SaveAsync(new(2,"Changed",null,null,true,false,1,null)));
        access.Permissions.Clear();await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service.SaveAsync(new(1,"Changed",null,null,true,false,1,null)));
    }
    [Fact]public async Task Framework_links_are_frozen_after_employment_history_exists()
    {
        await Seed();await using(var db=factory.CreateDbContext()){db.Jobs.Add(new(){JobId=1,OrganisationID=10,IndividualID=100});db.JobPositions.Add(new(){JobPositionId=1,JobId=1,PositionId=1,FromDate=DateTime.UtcNow.AddDays(-10),JobPositionStateId=4});await db.SaveChangesAsync();}
        Assert.False((await Service.SaveAsync(new(1,"Changed",null,null,true,false,2,1))).Success);
        Assert.True((await Service.SaveAsync(new(1,"Renamed",null,null,false,false,1,null))).Success);
    }
}
