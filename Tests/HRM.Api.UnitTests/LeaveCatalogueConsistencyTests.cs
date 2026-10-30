using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.LeaveTypes;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class LeaveCatalogueConsistencyTests
{
    private readonly QueryFactory factory=new(ignoreTransactions:true);
    private readonly FakeAccess access=new();
    private readonly IOperationLogService logs=StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=1}));

    [Theory]
    [InlineData(true,1,false,false)]
    [InlineData(false,20,true,false)]
    [InlineData(false,20,false,true)]
    public async Task Active_names_cannot_conflict_in_the_visible_catalogue(bool existingGlobal,int existingOrg,bool newGlobal,bool allowed)
    {
        access.Actor!.IsSuperAdministrator=true;
        await using(var db=factory.CreateDbContext())
        {
            db.RequestTypes.Add(new(){RequestTypeId=34,TypeName="Annual Leave Request"});
            db.LeaveTypes.Add(new(){LeaveTypeId=1,Name="Annual Leave",Code="AL01",OrganisationId=existingOrg,IsGlobal=existingGlobal,RequestTypeId=34});
            await db.SaveChangesAsync();
        }
        var catalogue=new LeaveTypeCatalogueService(factory,access,logs);
        var result=await catalogue.CreateAsync(new LeaveTypeCatalogueSaveRequest{Code="OTHER-AL",Name=" annual leave ",RequestTypeId=34,IsGlobal=newGlobal});
        Assert.Equal(allowed,result.Success);
        if(!allowed)Assert.Contains("already available",result.Message);
    }
    [Fact]
    public async Task Reactivating_an_archived_duplicate_is_rejected()
    {
        access.Actor!.IsSuperAdministrator=true;
        await using(var db=factory.CreateDbContext())
        {
            db.RequestTypes.Add(new(){RequestTypeId=34,TypeName="Annual Leave Request"});
            db.LeaveTypes.AddRange(new(){LeaveTypeId=1,Name="Annual Leave",Code="AL01",OrganisationId=1,IsGlobal=true,RequestTypeId=34},
                new(){LeaveTypeId=66,Name="Annual Leave",Code="LT-66",OrganisationId=10,IsActive=false,RequestTypeId=34});
            await db.SaveChangesAsync();
        }
        var catalogue=new LeaveTypeCatalogueService(factory,access,logs);
        var result=await catalogue.UpdateAsync(new(){LeaveTypeId=66,Code="LT-66",Name="Annual Leave",RequestTypeId=34,IsActive=true});
        Assert.False(result.Success);Assert.Contains("already available",result.Message);
    }
    [Fact]
    public async Task Leave_sets_and_accrual_only_offer_active_accessible_catalogue_types()
    {
        access.Actor!.IsSuperAdministrator=true;
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.Add(new(){BusinessEntityID=10,OrganisationName="Tenant"});
            db.JobTypes.Add(new(){JobTypeId=1,TypeName="Contract"});
            db.LeaveSets.Add(new(){LeaveSetId=1,OrganisationId=10,Name="Annual leave set"});
            db.LeaveTypes.AddRange(new(){LeaveTypeId=1,Code="AL01",Name="Annual Leave",OrganisationId=1,IsGlobal=true},
                new(){LeaveTypeId=66,Code="LT-66",Name="Annual Leave",OrganisationId=10,IsActive=false},
                new(){LeaveTypeId=70,Code="FOREIGN",Name="Foreign",OrganisationId=20});
            db.LeaveSetLeaveTypes.Add(new(){LeaveSetId=1,LeaveTypeId=66,IsValid=true});
            await db.SaveChangesAsync();
        }
        var sets=new LeaveSetConfigurationService(factory,access,logs);
        var contents=await sets.LoadContentsAsync(1);
        Assert.Equal(1,Assert.Single(contents.Types).Id);Assert.Empty(contents.Selected);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>sets.SaveContentsAsync(1,[66],0));
        var accrual=new LeaveAccrualSettingsService(factory,access,logs);
        var options=await accrual.LoadAsync();Assert.Equal(1,Assert.Single(options.LeaveTypes).Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>accrual.SaveAsync(new JobTypeLeaveAccrualRule{JobTypeId=1,LeaveTypeId=66,GrantDays=2.5m,FirstGrantAfterMonths=1,RepeatEveryMonths=1,CarryForwardMode="None"}));
    }
}
