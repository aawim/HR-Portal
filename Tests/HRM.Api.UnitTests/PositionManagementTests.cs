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
        db.PositionFrameworks.AddRange(new(){FrameworkId=1,Code="LEGACY",Name="Legacy",IsActive=true},new(){FrameworkId=2,Code="NEW_PAY",Name="Government",IsActive=true},new(){FrameworkId=3,Code="CUSTOM",Name="Non CSC / Custom",IsActive=true});
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
    [Fact]public async Task Custom_salary_is_effective_dated_and_existing_salary_history_cannot_be_overwritten()
    {
        await Seed();
        var input=new PositionInput(0,"Custom paid",null,null,true,false,3,null){BasicSalary=6000.25m,SalaryEffectiveDate=new(2026,1,1)};
        Assert.True((await Service.SaveAsync(input)).Success);
        var row=Assert.Single((await Service.LoadAsync()).Positions,x=>x.Name=="Custom paid");
        Assert.Equal(6000.25m,Assert.Single(row.Salaries).Amount);
        Assert.False((await Service.SaveAsync(input with{Id=row.Id,BasicSalary=7000})).Success);
        Assert.True((await Service.SaveAsync(input with{Id=row.Id,BasicSalary=7000,SalaryEffectiveDate=new(2026,2,1)})).Success);
        row=Assert.Single((await Service.LoadAsync()).Positions,x=>x.Id==row.Id);
        Assert.Equal(2,row.Salaries.Count);
        Assert.Equal(7000,row.Salaries[0].Amount);
    }
    [Fact]public async Task Csc_lookups_are_tenant_scoped_and_matrix_salary_cannot_be_manually_overridden()
    {
        await Seed();await using(var db=factory.CreateDbContext())
        {
            db.PositionTypes.Add(new(){PositionTypeId=1,Name="CSC",IsActive=true});
            db.PositionRanks.AddRange(new(){PositionRankId=1,Name="Local",OrganisationBusinessEntityID=10,IsActive=true},new(){PositionRankId=2,Name="Foreign",OrganisationBusinessEntityID=20,IsActive=true});
            db.PositionClassifications.Add(new(){PositionClassificationId=1,Name="Officer",OrganisationBusinessEntityID=10,IsActive=true});
            await db.SaveChangesAsync();
        }
        var input=new PositionInput(0,"CSC officer",null,null,true,false,1,null){TypeId=1,RankId=1,ClassificationId=1};
        Assert.True((await Service.SaveAsync(input)).Success);
        Assert.False((await Service.SaveAsync(input with{Name="Foreign rank",RankId=2})).Success);
        Assert.Single((await Service.LoadAsync()).Ranks);
        Assert.False((await Service.SaveAsync(new(0,"Matrix",null,null,true,false,2,1){BasicSalary=50,SalaryEffectiveDate=new(2026,1,1)})).Success);
        Assert.False((await Service.SaveAsync(input with{Name="Invalid salary",BasicSalary=-1,SalaryEffectiveDate=new(2026,1,1)})).Success);
    }
}
