using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
static class LeaveSetConfigurationTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Denied(Func<Task> action){try{await action();}catch(UnauthorizedAccessException){return;}throw new Exception("Expected tenant/permission denial");}
    static async Task Invalid(Func<Task> action){try{await action();}catch(InvalidOperationException){return;}throw new Exception("Expected validation denial");}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),o=>o.EnableNullChecks(false)).ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var actor=new UserContextDto{UserId=1,IndividualId=100};var permission=true;
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(actor),"GetCurrentOrganisationIdAsync"=>Task.FromResult(10),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! ==10),"HasPermissionAsync"=>Task.FromResult(permission&&(string)a[0]! =="Admin"),_=>throw new Exception(m)});
        var logs=Proxy.For<IOperationLogService>((m,a)=>m=="CreateAsync"?Audit((HrmTeContext)a[0]!):throw new Exception(m));
        await using(var db=factory.CreateDbContext()){db.Organisations.AddRange(new(){BusinessEntityID=10,OrganisationName="Current"},new(){BusinessEntityID=20,OrganisationName="Other"});db.LeaveSets.AddRange(new(){LeaveSetId=20,OrganisationId=20,Name="Foreign"},new(){LeaveSetId=21,OrganisationId=10,Name="Shared",IsGlobal=true});await db.SaveChangesAsync();}
        var service=new LeaveSetConfigurationService(factory,access,logs);
        Check((await service.LoadAsync()).Sets.All(s=>s.OrganisationId==10),"Leave set catalogue leaked tenants");
        await Denied(()=>service.SaveAsync(new(){OrganisationId=20,Name="Foreign write"}));
        await Denied(()=>service.SaveAsync(new(){LeaveSetId=20,OrganisationId=10,Name="Forged owner"}));
        await Denied(()=>service.DeleteAsync(new(){LeaveSetId=20,OrganisationId=20}));
        await Denied(()=>service.SaveAsync(new(){OrganisationId=10,Name="Shared privilege",IsGlobal=true}));
        await Denied(()=>service.SaveAsync(new(){LeaveSetId=21,OrganisationId=10,Name="Unshare"}));
        await service.SaveAsync(new(){OrganisationId=10,Name="Local set"});
        await Invalid(()=>service.SaveAsync(new(){OrganisationId=10,Name=" local SET "}));
        var row=(await service.LoadAsync()).Sets.Single(s=>s.Name=="Local set");Check(row.OperationLogId>0,"Leave set save was not audited");
        row.Name="Updated local set";await service.SaveAsync(row);await Invalid(()=>service.SaveAsync(row));
        row=(await service.LoadAsync()).Sets.Single(s=>s.Name=="Updated local set");
        await using(var db=factory.CreateDbContext()){db.GroupLeaveSets.Add(new(){GroupId=99,LeaveSetId=row.LeaveSetId,IsValid=true});await db.SaveChangesAsync();}
        await Invalid(()=>service.DeleteAsync(row));
        await service.SaveAsync(new(){OrganisationId=10,Name="Unused"});var unused=(await service.LoadAsync()).Sets.Single(s=>s.Name=="Unused");await service.DeleteAsync(unused);
        Check(!(await service.LoadAsync()).Sets.Any(s=>s.Name=="Unused"),"Unused set deletion failed");
        await using(var db=factory.CreateDbContext())
        {
            db.JobTypes.AddRange(new(){JobTypeId=1,TypeName="Contract",IsActive=true},new(){JobTypeId=2,TypeName="Ended",IsActive=false});
            db.LeaveTypes.AddRange(new(){LeaveTypeId=10,OrganisationId=10,Name="Annual"},new(){LeaveTypeId=20,OrganisationId=20,Name="Foreign private"},new(){LeaveTypeId=30,OrganisationId=20,Name="Shared sick",IsGlobal=true});await db.SaveChangesAsync();
        }
        await service.SaveAsync(new(){OrganisationId=10,Name="Package"});var package=(await service.LoadAsync()).Sets.Single(s=>s.Name=="Package");
        var contents=await service.LoadContentsAsync(package.LeaveSetId);
        Check(contents.Types.Select(t=>t.Id).Order().SequenceEqual([10,30]),"Leave type picker leaked private foreign types");
        await service.SaveContentsAsync(package.LeaveSetId,[10,30,10],contents.AuditId);
        await Invalid(()=>service.SaveContentsAsync(package.LeaveSetId,[10],contents.AuditId));
        contents=await service.LoadContentsAsync(package.LeaveSetId);Check(contents.Selected.Order().SequenceEqual([10,30]),"Leave type assignment was lost or duplicated");
        await Denied(()=>service.SaveContentsAsync(package.LeaveSetId,[20],contents.AuditId));await Denied(()=>service.LoadContentsAsync(20));
        await service.SaveContentsAsync(package.LeaveSetId,[10],contents.AuditId);
        await using(var db=factory.CreateDbContext())Check(await db.LeaveSetLeaveTypes.CountAsync(l=>l.LeaveSetId==package.LeaveSetId)==2&&!await db.LeaveSetLeaveTypes.AnyAsync(l=>l.LeaveSetId==package.LeaveSetId&&l.LeaveTypeId==30&&l.IsValid),"Unassigning a type lost history or kept it active");
        var assignment=await service.LoadJobTypeAsync(1);
        await Denied(()=>service.SaveJobTypeAsync(1,[20],assignment.Version));
        await Invalid(()=>service.SaveJobTypeAsync(1,[21],assignment.Version));
        await service.SaveJobTypeAsync(1,[package.LeaveSetId],assignment.Version);
        await Invalid(()=>service.SaveJobTypeAsync(1,[],assignment.Version));
        assignment=await service.LoadJobTypeAsync(1);Check(assignment.Selected.SequenceEqual([package.LeaveSetId])&&assignment.Sets.Single(s=>s.Id==package.LeaveSetId).Types.Single().Id==10,"Job Type did not use its selected set contents");
        contents=await service.LoadContentsAsync(package.LeaveSetId);await Invalid(()=>service.SaveContentsAsync(package.LeaveSetId,[],contents.AuditId));
        await service.SaveJobTypeAsync(1,[],assignment.Version);
        await using(var db=factory.CreateDbContext())Check(await db.JobTypeLeaveSets.CountAsync()==1&&!await db.JobTypeLeaveSets.AnyAsync(l=>l.IsActive),"Unassigning a set deleted history or left it active");
        package=(await service.LoadAsync()).Sets.Single(s=>s.Name=="Package");await Invalid(()=>service.DeleteAsync(package));
        await Invalid(()=>service.LoadJobTypeAsync(2));
        permission=false;await Denied(()=>service.LoadAsync());actor.IsSuperAdministrator=true;
        await service.SaveAsync(new(){OrganisationId=10,Name="Super shared",IsGlobal=true});Check((await service.LoadAsync()).IsSuperAdmin,"Super Administrator configuration scope failed");
    }
    static async Task<OperationLog> Audit(HrmTeContext db){var row=new OperationLog();db.OperationLogs.Add(row);await db.SaveChangesAsync();return row;}
}
