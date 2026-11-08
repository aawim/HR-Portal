using System.Text.Json;
using HRM.DTOs.Contracts;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services;
using Xunit;
using Contract=HRM.Services.Contracts.FormerStaffServiceContracts;

namespace HRM.Api.UnitTests;

public sealed class FormerStaffExportTests
{
    [Fact]public void Work_plan_generation_sources_are_exported_as_readable_text()
    {
        foreach(var value in System.Enum.GetValues<WorkPlanGenerationSource>())
        {
            var cell=FormerStaffService.ToCell(value);Assert.Equal(ExportCellKind.Text,cell.Kind);Assert.Equal(value.ToString(),cell.Text);
        }
        Assert.Throws<InvalidOperationException>(()=>FormerStaffService.ToCell(new object()));
    }
    [Fact]public async Task Former_staff_with_work_plans_loads_through_the_api_export_contract()
    {
        var factory=new QueryFactory();var access=new FakeAccess();access.Actor!.IsSuperAdministrator=true;
        var today=FormerStaffPolicy.Today;
        await using(var db=factory.CreateDbContext())
        {
            db.Individuals.Add(new(){BusinessEntityId=820,FirstNameEnglish="Former",LastNameEnglish="Staff"});
            db.Organisations.Add(new(){BusinessEntityID=10,OrganisationName="Tenant"});
            db.Jobs.Add(new(){JobId=50,IndividualID=820,OrganisationID=10,JobStateId=8,JoinedDate=today.AddMonths(-1),TerminatedDate=today});
            db.WorkPlans.Add(new(){WorkPlanId=1,IndividualId=820,JobId=50,OrganisationBusinessEntityId=10,WorkDate=today.AddDays(-1),GenerationSource=WorkPlanGenerationSource.Template,PlanGuid=Guid.NewGuid()});
            await db.SaveChangesAsync();
        }
        Contract.IOperations service=new FormerStaffService(factory,access);
        var report=await service.DetailAsync(820);var plans=Assert.Single(report.Tables,x=>x.Name=="Work plans");var row=Assert.Single(plans.Rows);
        Assert.Contains(row,x=>x.Kind==ExportCellKind.Text&&x.Text=="Template");
        var restored=JsonSerializer.Deserialize<ExportReportDto>(JsonSerializer.Serialize(report))!;Assert.Equal(report.Tables.Count,restored.Tables.Count);
        access.Organisation=20;await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.DetailAsync(820));
    }
}
