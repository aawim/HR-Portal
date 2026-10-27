using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
static class FormerStaffTests
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),x=>x.EnableNullChecks(false)).ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var today=FormerStaffPolicy.Today;
        await using(var db=factory.CreateDbContext())
        {
            db.Organisations.AddRange(new(){BusinessEntityID=10,OrganisationName="Tenant"},new(){BusinessEntityID=20,OrganisationName="Foreign"});
            db.OrganisationStructures.AddRange(new(){OrganisationStructureId=1,Name="Department",OrganisationBusinessEntityID=10},new(){OrganisationStructureId=2,Name="Foreign department",OrganisationBusinessEntityID=20},new(){OrganisationStructureId=3,Name="Second department",OrganisationBusinessEntityID=10});
            db.JobTypes.AddRange(new(){JobTypeId=1,TypeName="Contract"},new(){JobTypeId=2,TypeName="Permanent"});db.JobStates.Add(new(){JobStateId=4,StateName="Approved"});
            db.VerifiedStates.AddRange(new(){VerifiedStateId=1,StateName="Verified"},new(){VerifiedStateId=2,StateName="Not Verified"});
            db.BusinessEntityStates.AddRange(new(){BusinessEntityStateId=1,StateName="Active"},new(){BusinessEntityStateId=2,StateName="Inactive"});
            db.BusinessEntities.AddRange(new(){BusinessEntityID=100},new(){BusinessEntityID=200,VerifiedStateId=1,BusinessEntityStateId=2},new(){BusinessEntityID=300},new(){BusinessEntityID=400});
            db.Individuals.AddRange(new(){BusinessEntityId=100,FirstNameEnglish="Active"},new(){BusinessEntityId=200,FirstNameEnglish="Former",LastNameEnglish="Staff"},new(){BusinessEntityId=300,FirstNameEnglish="Position ended"},new(){BusinessEntityId=400,FirstNameEnglish="Foreign"});
            db.Staffs.AddRange(new(){IndividualId=100,EmployeeNumber="A"},new(){IndividualId=200,EmployeeNumber="F200"},new(){IndividualId=300,EmployeeNumber="F300"},new(){IndividualId=400});
            db.Jobs.AddRange(new(){JobId=1,IndividualID=100,OrganisationID=10,OrganisationStructureId=1,JobTypeId=1,JobStateId=4,JoinedDate=today.AddYears(-2)},new(){JobId=2,IndividualID=200,OrganisationID=10,OrganisationStructureId=1,JobTypeId=1,JobStateId=4,JoinedDate=today.AddYears(-2),TerminatedDate=today.AddDays(-1)},new(){JobId=3,IndividualID=300,OrganisationID=10,OrganisationStructureId=3,JobTypeId=2,JobStateId=4,JoinedDate=today.AddYears(-2)},new(){JobId=4,IndividualID=400,OrganisationID=20,OrganisationStructureId=2,JobTypeId=1,JobStateId=4,JoinedDate=today.AddYears(-2),TerminatedDate=today.AddDays(-1)});
            db.JobPositions.Add(new(){JobPositionId=3,JobId=3,PositionId=1,JobPositionStateId=4,FromDate=today.AddYears(-2),ToDate=today.AddDays(-2)});
            db.BussinessEntityContactInformations.Add(new(){BussinessEntityContactInformationId=1,BussinessEntityId=200,Value="original"});
            db.JobLeaveTypes.Add(new(){JobLeaveTypeId=2,JobId=2,RemainingDays=15});
            db.Requests.Add(new(){RequestId=2,ApplicantBusinessEntityId=200,OrganisationBusinessEntityID=10});
            await db.SaveChangesAsync();
        }
        var org=10;var super=true;
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=1,IndividualId=100,IsSuperAdministrator=super}),"GetCurrentOrganisationIdAsync"=>Task.FromResult(org),"HasOrganisationAccessAsync"=>Task.FromResult(true),"HasPermissionAsync"=>Task.FromResult(false),_=>throw new Exception(m)});
        var service=new FormerStaffService(factory,access);
        var rows=await service.SearchAsync();Check(rows.Staff.Select(r=>r.Id).Order().SequenceEqual([200,300]),"Former staff omitted ended positions, included active staff or leaked tenants");
        Check((await service.SearchAsync("Former Staff")).Staff.Single().Id==200,"Former name search failed");
        Check((await service.SearchAsync("F300")).Staff.Single().Id==300,"Former employee-number search failed");
        Check((await service.SearchAsync(filters:new(DepartmentId:1))).Staff.Single().Id==200,"Department filter failed");
        Check((await service.SearchAsync(filters:new(JobTypeId:2))).Staff.Single().Id==300,"Job Type filter failed");
        Check((await service.SearchAsync(filters:new(From:today.AddDays(-2),To:today.AddDays(-2)))).Staff.Single().Id==300,"Inclusive end-date filter failed");
        Check((await service.SearchAsync(filters:new(VerifiedStateId:1))).Staff.Single().Id==200,"Verification state filter failed");
        Check((await service.SearchAsync(filters:new(VerifiedStateId:-1))).Staff.Single().Id==300,"Unset verification filter failed");
        Check((await service.SearchAsync(filters:new(BusinessEntityStateId:2))).Staff.Single().Id==200,"Entity state filter failed");
        var combined=await service.SearchAsync("Former",filters:new(1,1,today.AddDays(-1),today.AddDays(-1),1,2));
        Check(combined.Staff.Single().Id==200&&combined.Staff[0].VerificationState=="Verified"&&combined.Staff[0].EntityState=="Inactive","Combined filters/state display failed");
        var empty=await service.SearchAsync(filters:new(VerifiedStateId:2));Check(empty.Staff.Count==0&&empty.Departments.Count==2&&empty.JobTypes.Count==2&&empty.VerifiedStates.Count==2,"Empty filters removed stable All/lookup options");
        var blocked=false;try{await service.SearchAsync(filters:new(DepartmentId:2));}catch(InvalidOperationException){blocked=true;}Check(blocked,"Foreign department filter accepted");
        blocked=false;try{await service.SearchAsync(filters:new(From:today,To:today.AddDays(-1)));}catch(InvalidOperationException){blocked=true;}Check(blocked,"Reversed date range accepted");
        var report=await service.DetailAsync(200);
        Check(report.Tables.Any(t=>t.Name=="Employment history"&&t.Rows.Count==1)&&report.Tables.Any(t=>t.Name=="Contacts"&&t.Rows.Count==1),"Former staff history was unavailable");
        var excel=AttendanceReportExport.Excel(report);Check(excel.Length>1000&&excel[0]=='P'&&excel[1]=='K',"Former information Excel export failed");
        var pdf=AttendanceReportExport.Pdf(report,Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","fonts","Faruma.ttf"));Check(pdf.Length>1000&&System.Text.Encoding.ASCII.GetString(pdf,0,4)=="%PDF","Former information PDF export failed");
        foreach(var edit in new Func<HrmTeContext,Task>[] {
            async db=>(await db.Individuals.SingleAsync(i=>i.BusinessEntityId==200)).FirstNameEnglish="Changed",
            async db=>(await db.Jobs.SingleAsync(j=>j.JobId==2)).TerminatedDate=null,
            async db=>db.Jobs.Remove(await db.Jobs.SingleAsync(j=>j.JobId==2)),
            async db=>(await db.JobLeaveTypes.SingleAsync()).RemainingDays=99,
            async db=>(await db.BussinessEntityContactInformations.SingleAsync()).BussinessEntityId=100,
            async db=>(await db.Requests.SingleAsync()).StateChangeRemarks="Changed",
            db=>{db.AttendanceLogs.Add(new(){IndividualId=200,OrganisationId=10,Date=today});return Task.CompletedTask;},
            db=>{db.JobPositions.Add(new(){JobId=2,FromDate=today,JobPositionStateId=4});return Task.CompletedTask;}
        })
        {
            await using var db=factory.CreateDbContext();await edit(db);var writeBlocked=false;try{await db.SaveChangesAsync();}catch(InvalidOperationException ex)when(ex.Message==FormerStaffPolicy.ReadOnlyMessage){writeBlocked=true;}Check(writeBlocked,"Superadmin bypassed former staff immutability");
        }
        await using(var db=factory.CreateDbContext()){(await db.Individuals.SingleAsync(i=>i.BusinessEntityId==100)).FirstNameEnglish="Active edited";await db.SaveChangesAsync();}
        org=20;Check((await service.SearchAsync()).Staff.Single().Id==400,"Former staff current-tenant boundary failed");
        var denied=false;try{await service.DetailAsync(200);}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Former details leaked across tenants");
        super=false;denied=false;try{await service.SearchAsync();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Unauthorised user could browse former staff");
    }
}
