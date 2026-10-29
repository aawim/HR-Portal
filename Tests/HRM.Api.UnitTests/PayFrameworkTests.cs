using HRM.Application.Contracts.Pages;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class PayFrameworkTests
{
    private readonly QueryFactory factory=new(ignoreTransactions:true);
    private readonly FakeAccess access=new();
    private PositionManagementService Service=>new(factory,access,StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=1})));
    private static JobMatrixInput Input(string code="ENG")=>new(0,2,code,"Engineering",1,10000,new(2026,1,1),null,true,["Software Engineer","Systems Analyst"],[new(1,1000,null),new(2,1500,12000)],[new("Transport",500)]){Profile=new(){JobFamilyId=1,ProfessionId=1}};
    private async Task Seed()
    {
        access.Actor!.IsSuperAdministrator=true;
        await using var db=factory.CreateDbContext();
        db.PositionFrameworks.Add(new(){FrameworkId=2,Code="NEW_PAY",Name="New framework",IsActive=true});
        db.PayJobFamilies.Add(new(){JobFamilyId=1,Name="Engineering service",IsActive=true});db.PayProfessions.Add(new(){Id=1,JobFamilyId=1,Name="Engineering",IsActive=true});db.FinanceCurrencies.Add(new(){CurrencyTypeId=1,Name="MVR"});await db.SaveChangesAsync();
    }
    [Fact]public async Task Shared_framework_and_job_matrix_creation_requires_superadministrator()
    {
        await Seed();access.Actor!.IsSuperAdministrator=false;access.Permissions.Add("Positions.Configure");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service.SaveFrameworkAsync(new(0,"CUSTOM","Custom",true)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service.SaveJobMatrixAsync(Input()));
        Assert.False((await Service.LoadPayFrameworksAsync()).CanManageFrameworks);
        access.Actor.IsSuperAdministrator=true;
        Assert.True((await Service.SaveFrameworkAsync(new(0,"CUSTOM","Non CSC / Custom",true))).Success);
        Assert.Equal(2,(await Service.LoadPayFrameworksAsync()).Frameworks.Count);
    }
    [Fact]public async Task Study_field_suggestions_are_family_linked_and_custom_text_remains_valid()
    {
        await Seed();await using(var db=factory.CreateDbContext())
        {
            db.PayStudyFields.AddRange(new(){JobFamilyId=1,Name="Computer Science",IsActive=true},new(){JobFamilyId=1,Name="Retired example",IsActive=false});await db.SaveChangesAsync();
        }
        var fields=(await Service.LoadPayFrameworksAsync()).StudyFields;var field=Assert.Single(fields);Assert.Equal(1,field.FamilyId);Assert.Equal("Computer Science",field.Name);
        var input=Input();Assert.True((await Service.SaveJobMatrixAsync(input with{Profile=input.Profile with{FieldsOfStudy="Computer Science\nCustom interdisciplinary study"}})).Success);
        Assert.Contains("Custom interdisciplinary study",Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices).Profile.FieldsOfStudy);
    }
    [Fact]public async Task Sector_experience_suggestions_preserve_general_and_family_scopes_and_custom_text()
    {
        await Seed();await using(var db=factory.CreateDbContext())
        {
            db.PayExperienceAreas.AddRange(new(){Sector="Corporate",Name="Governance",IsActive=true},new(){JobFamilyId=1,Sector="Technical",Name="Systems",IsActive=true},new(){JobFamilyId=1,Sector="Technical",Name="Inactive",IsActive=false});await db.SaveChangesAsync();
        }
        var choices=(await Service.LoadPayFrameworksAsync()).ExperienceAreas;Assert.Equal(2,choices.Count);Assert.Contains(choices,x=>x.FamilyId==null);Assert.Contains(choices,x=>x.FamilyId==1);
        var input=Input();Assert.True((await Service.SaveJobMatrixAsync(input with{Profile=input.Profile with{ExperienceAreas="Governance\nCustom practical experience"}})).Success);
        Assert.Contains("Custom practical experience",Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices).Profile.ExperienceAreas);
    }
    [Fact]public async Task Profession_selection_controls_matrix_name_and_rejects_wrong_family()
    {
        await Seed();var input=Input() with{Name="Forged free text"};
        Assert.False((await Service.SaveJobMatrixAsync(input with{Profile=input.Profile with{JobFamilyId=999}})).Success);
        Assert.False((await Service.SaveJobMatrixAsync(input with{Profile=input.Profile with{ProfessionId=null}})).Success);
        Assert.True((await Service.SaveJobMatrixAsync(input)).Success);
        var data=await Service.LoadPayFrameworksAsync();var saved=Assert.Single(data.Matrices);Assert.Equal("Engineering",saved.Name);Assert.Equal(1,Assert.Single(data.Professions).FamilyId);
        Assert.False((await Service.SaveJobMatrixAsync(input with{Id=saved.Id,Profile=input.Profile with{ProfessionId=null}})).Success);
    }
    [Fact]public async Task Job_matrix_titles_steps_benefits_and_currency_are_inherited_by_tenant_position()
    {
        await Seed();Assert.True((await Service.SaveJobMatrixAsync(Input())).Success);
        var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        Assert.Equal(2,matrix.Titles.Count);Assert.Equal(10000,matrix.Steps[0].BasicSalary);Assert.Equal(12000,matrix.Steps[1].BasicSalary);Assert.Equal(1500,matrix.Steps[1].JobAllowance);Assert.Equal(500,Assert.Single(matrix.Benefits).Amount);
        access.Actor!.IsSuperAdministrator=false;access.Permissions.Add("Positions.Configure");
        Assert.True((await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,2,new(2026,1,1),null))).Success);
        var position=Assert.Single((await Service.LoadAsync()).Positions);Assert.Equal(2,position.FrameworkId);Assert.False(position.Global);Assert.Equal(10000,Assert.Single((await Service.LoadAsync()).Matrices).BasicSalary);
        await using(var db=factory.CreateDbContext()){Assert.Equal(10,(await db.PositionMatrices.SingleAsync()).OrganisationId);Assert.Single(db.FrameworkPositionMatrixHistories);}
        access.Organisation=20;Assert.Empty((await Service.LoadAsync()).Positions);
        Assert.True((await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,1,new(2026,1,1),null))).Success);
    }
    [Fact]public async Task Invalid_steps_duplicate_codes_foreign_titles_and_edits_to_used_matrices_are_rejected()
    {
        await Seed();Assert.False((await Service.SaveJobMatrixAsync(Input() with{Steps=[new(1,0,null),new(1,0,null)]})).Success);
        Assert.True((await Service.SaveJobMatrixAsync(Input())).Success);
        Assert.False((await Service.SaveJobMatrixAsync(Input())).Success);
        var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        Assert.False((await Service.CreatePositionMatrixAsync(new(matrix.Id,999,1,new(2026,1,1),null))).Success);
        Assert.True((await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,1,new(2026,1,1),null))).Success);
        Assert.False((await Service.SaveJobMatrixAsync(Input() with{Id=matrix.Id,BasicSalary=999})).Success);
        Assert.False((await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,1,new(2026,2,1),null))).Success);
    }
    [Fact]public async Task Approval_enforces_slot_capacity_and_snapshots_selected_pay_without_a_new_job()
    {
        await Seed();await Service.SaveJobMatrixAsync(Input());var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,1,new(2026,1,1),null));
        await using var db=factory.CreateDbContext();var position=await db.Positions.SingleAsync();
        db.Jobs.Add(new(){JobId=50,OrganisationID=10,IndividualID=500,JobStateId=4,JoinedDate=new(2026,1,1)});
        var assignment=new JobPosition{JobId=50,PositionId=position.PositionId,FromDate=new(2026,2,1),JobPositionStateId=4};db.JobPositions.Add(assignment);await db.SaveChangesAsync();
        await PositionMatrixAssignments.SnapshotAsync(db,10,assignment,1,selectedStepId:matrix.Steps[1].Id);await db.SaveChangesAsync();
        var pay=await db.FrameworkJobStepHistories.SingleAsync();Assert.Equal(50,pay.JobId);Assert.Equal(12000,pay.BasicSalarySnapshot);Assert.Equal(1500,pay.JobAllowanceSnapshot);Assert.Single(db.Jobs);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PositionMatrixAssignments.ValidateAsync(db,10,position.PositionId,new(2026,3,1),null));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PositionMatrixAssignments.ValidateAsync(db,20,position.PositionId,new(2026,3,1),null));
        var summary=await PositionMatrixAssignments.SummaryAsync(db,position.PositionId,matrix.Steps[1].Id);Assert.NotNull(summary);Assert.Equal(12000,summary.BasicSalary);Assert.Equal(500,Assert.Single(summary.Benefits).Amount);
    }
    [Fact]public async Task Nonoverlapping_occupancy_does_not_consume_multiple_slots_at_once()
    {
        await Seed();await Service.SaveJobMatrixAsync(Input());var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,2,new(2026,1,1),null));
        await using var db=factory.CreateDbContext();var position=await db.Positions.SingleAsync();
        db.JobPositions.AddRange(new(){PositionId=position.PositionId,JobId=1,FromDate=new(2026,1,1),ToDate=new(2026,2,1),JobPositionStateId=4},new(){PositionId=position.PositionId,JobId=2,FromDate=new(2026,2,1),ToDate=new(2026,3,1),JobPositionStateId=4});await db.SaveChangesAsync();
        Assert.NotNull(await PositionMatrixAssignments.ValidateAsync(db,10,position.PositionId,new(2026,1,1),new(2026,3,1)));
    }
    [Fact]public async Task Matrix_effective_dates_and_zero_slot_capacity_are_enforced()
    {
        await Seed();await Service.SaveJobMatrixAsync(Input() with{To=new(2026,12,31)});
        var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        Assert.False((await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,1,new(2026,1,1),null))).Success);
        Assert.True((await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,1,new(2026,1,1),new(2026,6,30)))).Success);
        await using var db=factory.CreateDbContext();var position=await db.Positions.SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PositionMatrixAssignments.ValidateAsync(db,10,position.PositionId,new(2026,7,1),null));
        Assert.NotNull(await PositionMatrixAssignments.ValidateAsync(db,10,position.PositionId,new(2026,2,1),new(2026,3,1)));
        (await db.PositionMatrices.SingleAsync()).ApprovedSlots=0;await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PositionMatrixAssignments.ValidateAsync(db,10,position.PositionId,new(2026,2,1),new(2026,3,1)));
    }
    [Fact]public async Task Date_changes_cannot_overlap_another_approved_pay_period()
    {
        await Seed();await Service.SaveJobMatrixAsync(Input());var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,2,new(2026,1,1),null));
        var today=DateTime.UtcNow.AddHours(5).Date;var start=today.AddDays(-10);var end=today.AddDays(30);
        HRM.Services.Contracts.StaffPositionManagementServiceContracts.Assignment original;
        await using(var db=factory.CreateDbContext())
        {
            var position=await db.Positions.SingleAsync();db.Jobs.Add(new(){JobId=50,IndividualID=500,OrganisationID=10,JobStateId=4,JoinedDate=new(2026,1,1)});
            var assignment=new JobPosition{JobId=50,PositionId=position.PositionId,JobPositionStateId=4,FromDate=start,ToDate=end};db.JobPositions.Add(assignment);await db.SaveChangesAsync();
            db.FrameworkJobStepHistories.AddRange(new(){JobId=50,PositionMatrixId=position.PositionMatrixId!.Value,JobMatrixStepId=matrix.Steps[0].Id,EffectiveFrom=start,EffectiveTo=end},new(){JobId=50,PositionMatrixId=position.PositionMatrixId.Value,JobMatrixStepId=matrix.Steps[0].Id,EffectiveFrom=end,EffectiveTo=end.AddDays(30)});await db.SaveChangesAsync();
            original=new(assignment.JobPositionId,50,position.PositionId,position.Name,start,end,4,"Approved",0);
        }
        var service=new StaffPositionManagementService(factory,access,null!,null!,null!,null!,null!);
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(500,50,original.PositionId,start,end.AddDays(10),"Extend period",original));
        Assert.Contains("overlap another approved pay-step period",error.Message);
    }
    [Fact]public async Task Employees_sharing_one_position_can_have_different_job_pay_steps()
    {
        await Seed();await Service.SaveJobMatrixAsync(Input());var matrix=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        await Service.CreatePositionMatrixAsync(new(matrix.Id,matrix.Titles[0].Id,2,new(2026,1,1),null));
        await using var db=factory.CreateDbContext();var position=await db.Positions.SingleAsync();Assert.Null((await db.PositionMatrices.SingleAsync()).JobMatrixStepId);Assert.DoesNotContain("Step",position.Name);
        var first=new JobPosition{JobId=50,PositionId=position.PositionId,FromDate=new(2026,2,1),JobPositionStateId=4};var second=new JobPosition{JobId=51,PositionId=position.PositionId,FromDate=new(2026,2,1),JobPositionStateId=4};
        db.JobPositions.AddRange(first,second);await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PositionMatrixAssignments.SnapshotAsync(db,10,first,1));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>PositionMatrixAssignments.SnapshotAsync(db,10,first,1,selectedStepId:999));
        await PositionMatrixAssignments.SnapshotAsync(db,10,first,1,selectedStepId:matrix.Steps[0].Id);await db.SaveChangesAsync();
        await PositionMatrixAssignments.SnapshotAsync(db,10,second,1,selectedStepId:matrix.Steps[1].Id);await db.SaveChangesAsync();
        Assert.Equal(10000,(await db.FrameworkJobStepHistories.SingleAsync(x=>x.JobId==50)).BasicSalarySnapshot);Assert.Equal(12000,(await db.FrameworkJobStepHistories.SingleAsync(x=>x.JobId==51)).BasicSalarySnapshot);
        Assert.Single(db.Positions);Assert.Equal(2,await db.FrameworkJobStepHistories.CountAsync());
    }
    [Fact]public async Task Illustrated_profile_roundtrips_and_existing_requirement_rows_are_preserved()
    {
        await Seed();var input=Input() with{Profile=new(){JobFamilyId=1,ProfessionId=1,Description="Student counselling",PublicServiceRank="9",JobFamily="Educational Service",InstitutionalCategory="3",MisscoCode="1345",SkillLevel=4,AttendanceBenefitDaily=203,RevisedDate=new(2023,4,30),LicensesAndTraining="A specific license if required",Qualifications="MNQF level 9 and four years experience OR level 7/8 and eight years",FieldsOfStudy="School Counselling",ExperienceAreas="Managerial or technical counselling",Duties="Develop policies and provide counselling"}};
        Assert.True((await Service.SaveJobMatrixAsync(input)).Success);var row=Assert.Single((await Service.LoadPayFrameworksAsync()).Matrices);
        Assert.Equal(203,row.Profile.AttendanceBenefitDaily);Assert.Equal("1345",row.Profile.MisscoCode);Assert.Equal(input.Profile.Qualifications,row.Profile.Qualifications);
        Assert.False((await Service.SaveJobMatrixAsync(input with{Id=row.Id,Profile=row.Profile with{Qualifications="Overwrite existing pathway"}})).Success);
        await using var db=factory.CreateDbContext();Assert.Equal(input.Profile.Qualifications,(await db.FrameworkMatrixQualifications.SingleAsync()).RequirementText);Assert.Equal(input.Profile.Duties,(await db.FrameworkMatrixDuties.SingleAsync()).DutyDescription);
    }
}
