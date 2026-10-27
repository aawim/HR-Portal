using System.Reflection;
using HRM.Models;
using HRM.DTOs.UserContext;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
internal static class FormerStaffSqlSmoke
{
    public static async Task RunAsync(HrmTeContext db)
    {
        var connection=db.Database.GetConnectionString()!;
        if(!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("HRM_LeaveUpgrade_",StringComparison.Ordinal))throw new InvalidOperationException("Former staff smoke requires an isolated restored database.");
        var current=FormerStaffPolicy.CurrentJobs(db);
        var record=await FormerStaffPolicy.EndedJobs(db).Where(j=>!current.Any(a=>a.IndividualID==j.IndividualID&&a.OrganisationID==j.OrganisationID)).Select(j=>new{Person=j.IndividualID,Org=j.OrganisationID}).FirstOrDefaultAsync()??throw new InvalidOperationException("No historical former staff record available for SQL verification.");
        var access=DispatchProxy.Create<IUserAccessService,ChatSmokeAccess>();
        ((ChatSmokeAccess)(object)access).Handler=(m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=0,IndividualId=0,IsSuperAdministrator=true}),"GetCurrentOrganisationIdAsync"=>Task.FromResult(record.Org),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! ==record.Org),"HasPermissionAsync"=>Task.FromResult(false),_=>throw new InvalidOperationException(m)};
        var factory=new Factory(connection);var service=new FormerStaffService(factory,access);
        var result=await service.SearchAsync();if(!result.Staff.Any(r=>r.Id==record.Person))throw new InvalidOperationException("Former staff SQL search omitted the historical person.");
        var report=await service.DetailAsync(record.Person);
        if(!report.Tables.Any(t=>t.Name=="Employment history"&&t.Rows.Count>0))throw new InvalidOperationException("Former SQL employment history unavailable.");
        var bytes=AttendanceReportExport.Excel(report);if(bytes.Length<1000)throw new InvalidOperationException("Former SQL export empty.");
        var attendance=new AttendanceService(factory,null!,null!,access,null!,Microsoft.Extensions.Logging.Abstractions.NullLogger<AttendanceService>.Instance);
        var from=FormerStaffPolicy.Today.AddDays(-30);var days=await attendance.GetStaffYearlyAttendanceAsync(record.Person,FormerStaffPolicy.Today.Year);
        await attendance.GetStaffClockEventsAsync(record.Person,from,FormerStaffPolicy.Today);
        await using(var attempt=factory.CreateDbContext())
        {
            var individual=await attempt.Individuals.SingleAsync(i=>i.BusinessEntityId==record.Person);individual.FirstNameEnglish="BLOCKED FORMER STAFF TEST";
            var denied=false;try{await attempt.SaveChangesAsync();}catch(InvalidOperationException ex)when(ex.Message==FormerStaffPolicy.ReadOnlyMessage){denied=true;}if(!denied)throw new InvalidOperationException("Former SQL write protection failed.");
        }
        Console.WriteLine($"PASS isolated SQL former staff: {result.Staff.Count} authorised records; full history, attendance queries, Excel export and superadmin write denial.");
    }
    private sealed class Factory(string connection):IDbContextFactory<HrmTeContext>
    {public HrmTeContext CreateDbContext()=>new(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);}
}
