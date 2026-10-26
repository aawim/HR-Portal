using HRM.DTOs.UserContext;
using HRM.DTOs.Organisation;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

static class SupervisorOrganisationScopeTests
{
    static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    static async Task Denied(Func<Task> action) { try { await action(); } catch(UnauthorizedAccessException) { return; } throw new Exception("Expected current-organisation denial"); }
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),o=>o.EnableNullChecks(false)).Options);
        var super=false;var current=10;
        // Broad permissions and multi-organisation membership must not bypass current-tenant scope.
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch {
            "RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=1,IndividualId=100,IsSuperAdministrator=super}),
            "HasPermissionAsync" or "HasOrganisationAccessAsync"=>Task.FromResult(true),
            "GetCurrentOrganisationIdAsync"=>Task.FromResult(current),_=>throw new Exception(m) });
        var service=new OrganisationSettingsService(factory,access,null!);
        await using(var db=factory.CreateDbContext()) { db.Organisations.AddRange(new(){BusinessEntityID=10,OrganisationName="Current"},new(){BusinessEntityID=20,OrganisationName="Other"});await db.SaveChangesAsync(); }
        var today=DateTime.UtcNow.AddHours(5).Date;
        await using(var db=factory.CreateDbContext())
        {
            db.OrganisationStructureTypes.Add(new(){OrganisationStructureTypeId=1,TypeName="Department"});
            db.OrganisationStructures.AddRange(new(){OrganisationStructureId=101,OrganisationBusinessEntityID=10,OrganisationStructureStateId=3,OrganisationStructureTypeId=1,Name="Department A"},new(){OrganisationStructureId=201,OrganisationBusinessEntityID=20,OrganisationStructureStateId=3,OrganisationStructureTypeId=1,Name="Other department"});
            db.Teams.AddRange(new(){TeamId=10,OrganisationId=10,IsValid=true,Name="Current team",StartDate=today.AddDays(-5)},new(){TeamId=20,OrganisationId=20,IsValid=true,Name="Other team",StartDate=today.AddDays(-5)});
            foreach(var person in new[]{100,200,300,400,500}){db.Individuals.Add(new(){BusinessEntityId=person,FirstNameEnglish="Staff",LastNameEnglish=person.ToString()});db.Staffs.Add(new(){IndividualId=person});}
            db.Jobs.AddRange(new(){JobId=100,IndividualID=100,OrganisationID=10,OrganisationStructureId=101,JobStateId=4,JoinedDate=today.AddDays(-5)},
                new(){JobId=101,IndividualID=100,OrganisationID=10,OrganisationStructureId=101,JobStateId=4,JoinedDate=today.AddDays(-10)},
                new(){JobId=200,IndividualID=200,OrganisationID=10,OrganisationStructureId=101,JobStateId=4,JoinedDate=today.AddDays(-10),TerminatedDate=today.AddDays(-1)},
                new(){JobId=300,IndividualID=300,OrganisationID=20,OrganisationStructureId=101,JobStateId=4,JoinedDate=today.AddDays(-10)},
                new(){JobId=400,IndividualID=400,OrganisationID=10,OrganisationStructureId=101,JobStateId=4,JoinedDate=today.AddDays(-10)},
                new(){JobId=500,IndividualID=500,OrganisationID=10,OrganisationStructureId=101,JobStateId=4,JoinedDate=today.AddDays(-10)});
            db.TeamStaffs.AddRange(new(){TeamId=10,StaffId=100,IsValid=true,StartDate=today.AddDays(-2)},new(){TeamId=10,StaffId=100,IsValid=true,StartDate=today.AddDays(-3)},new(){TeamId=10,StaffId=200,IsValid=true,StartDate=today.AddDays(-2)},new(){TeamId=10,StaffId=300,IsValid=true,StartDate=today.AddDays(-2)},new(){TeamId=10,StaffId=400,IsValid=true,StartDate=today.AddDays(1)},new(){TeamId=10,StaffId=500,IsValid=true,StartDate=today.AddDays(-5),EndDate=today.AddDays(-1)});
            await db.SaveChangesAsync();
        }
        var directory=await service.GetTeamSettingsAsync(10);
        Check(directory.Rows.Single(r=>r.Kind=="Department").Members.Select(m=>m.Id).Order().SequenceEqual([100,400,500]),"Department staff were duplicated, foreign or no longer employed");
        var teamStaff=directory.Rows.Single(r=>r.Kind=="Team").Members;
        Check(teamStaff.Count==1&&teamStaff[0].Id==100&&teamStaff[0].Name=="Staff 100","Team staff included duplicates, expired/future membership or another organisation");
        Check((await service.GetTeamOrganisationsAsync()).Select(o=>o.Id).SequenceEqual([10]),"Admin was offered another organisation");
        Check((await service.LoadAsync()).Organisations.Select(o=>o.Id).SequenceEqual([10]),"Organisation settings exposed another organisation");
        await service.GetTeamSettingsAsync(10);
        await Denied(()=>service.GetTeamSettingsAsync(20));
        await Denied(()=>service.GetLinksAsync(20));
        await Denied(()=>service.SetTeamSupervisorAsync(20,"Department",1,100,true));
        await Denied(()=>service.SetTeamSupervisorAsync(20,"Team",1,100,false));
        await Denied(()=>service.SaveDetailsAsync(new OrganisationSettingsDto{Id=20,Name="Changed"}));
        await Denied(()=>service.SaveDetailsAsync(new OrganisationSettingsDto{Id=0,Name="Another organisation"}));
        current=20;Check((await service.GetTeamOrganisationsAsync()).Single().Id==20,"Current context switch was not respected");
        await Denied(()=>service.GetTeamSettingsAsync(10));
        current=0;await Denied(()=>service.GetTeamOrganisationsAsync());
        super=true;Check((await service.GetTeamOrganisationsAsync()).Count==2,"Super Administrator lost organisation selection");
        await service.GetTeamSettingsAsync(20);
        Check((await service.LoadAsync()).Organisations.Count==2,"Super Administrator organisation management lost global scope");
    }
}
