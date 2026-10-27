using System.Reflection;
using HRM.Models;
using HRM.DTOs.UserContext;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

internal static class OvertimePayrollSqlSmoke
{
    public static async Task RunAsync(HrmTeContext db)
    {
        var connection=db.Database.GetConnectionString()!;
        if(!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("HRM_LeaveUpgrade_",StringComparison.Ordinal))throw new InvalidOperationException("Payroll smoke requires an isolated restored database.");
        var today=DateTime.UtcNow.AddHours(5).Date;
        var users=await db.Users.AsNoTracking().Where(u=>u.IsActive&&u.LocalUserStateId==1&&u.UserOrganisation!=null&&db.Jobs.Any(j=>j.IndividualID==u.BusinessEntityID&&j.OrganisationID==u.UserOrganisation.BusinessEntityID&&j.JobStateId==4&&j.TerminatedDate==null&&j.JoinedDate<=today))
            .Select(u=>new{u.UserId,Person=u.BusinessEntityID,Org=u.UserOrganisation!.BusinessEntityID}).ToListAsync();
        var pair=users.GroupBy(x=>x.Org).First(g=>g.Select(x=>x.Person).Distinct().Count()>1).DistinctBy(x=>x.Person).Take(2).ToArray();
        var actor=pair[0];var staff=pair[1];
        var job=await db.Jobs.FirstAsync(j=>j.IndividualID==staff.Person&&j.OrganisationID==staff.Org&&j.JobStateId==4&&j.TerminatedDate==null);
        var log=await db.OperationLogs.FirstAsync();var type=await db.RequestTypes.SingleAsync(t=>t.TypeName==OvertimeRequestService.TypeName);
        var request=new Request{RequestTypeId=type.RequestTypeId,RequestStateId=4,ApplicantBusinessEntityId=staff.Person,OrganisationBusinessEntityID=staff.Org,ApplicationDate=DateTime.UtcNow,LastStateChangedDate=DateTime.UtcNow,OperationLogId=log.OperationLogId,ReferenceNumber="OT-SQL-PAYROLL-TEST"};
        db.Requests.Add(request);await db.SaveChangesAsync();
        var claim=new OvertimeRequest{RequestId=request.RequestId,OrganisationId=staff.Org,IndividualId=staff.Person,JobId=job.JobId,WorkDate=today.AddDays(-1),WeekStart=today.AddDays(-7),Minutes=60,Reason="Isolated payroll integration test",ApprovedPolicyJson="{}",CreatedAtUtc=DateTime.UtcNow};
        db.OvertimeRequests.Add(claim);await db.SaveChangesAsync();
        var access=DispatchProxy.Create<IUserAccessService,ChatSmokeAccess>();
        ((ChatSmokeAccess)(object)access).Handler=(m,a)=>m switch{
            "RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=actor.UserId,IndividualId=actor.Person}),
            "GetCurrentOrganisationIdAsync"=>Task.FromResult(actor.Org),"HasOrganisationAccessAsync"=>Task.FromResult((int)a[0]! == actor.Org),"HasPermissionAsync"=>Task.FromResult(true),_=>throw new InvalidOperationException(m)};
        var logs=DispatchProxy.Create<IOperationLogService,ChatSmokeAccess>();
        ((ChatSmokeAccess)(object)logs).Handler=(m,a)=>m=="CreateAsync"?Task.FromResult(log):throw new InvalidOperationException(m);
        var service=new OvertimeRequestService(new Factory(connection),access,null!,logs,null!,null!,null!);
        var key=Guid.NewGuid();
        var result=await service.ProcessPayrollAsync([request.RequestId],today,"SQL-SMOKE",key);
        if(!result.Success)throw new InvalidOperationException(result.Message);
        if(!(await service.ProcessPayrollAsync([request.RequestId],today,"SQL-SMOKE",key)).Success)throw new InvalidOperationException("SQL idempotency failed.");
        if((await service.ProcessPayrollAsync([request.RequestId],today,"DUPLICATE",Guid.NewGuid())).Success)throw new InvalidOperationException("SQL duplicate processing accepted.");
        if((await service.ActAsync(request.RequestId,4,"Cancel","Locked")).Success)throw new InvalidOperationException("SQL service payroll lock failed.");
        var blocked=false;
        try{await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Requests SET RequestStateID=5 WHERE RequestID={request.RequestId}");}
        catch(SqlException ex)when(ex.Number==51020){blocked=true;}
        if(!blocked||await db.Requests.AsNoTracking().Where(x=>x.RequestId==request.RequestId).Select(x=>x.RequestStateId).SingleAsync()!=4)throw new InvalidOperationException("SQL trigger did not preserve approved state.");
        var count=await db.OvertimePayrollItems.CountAsync(x=>x.RequestId==request.RequestId);
        if(count!=1)throw new InvalidOperationException("SQL payroll item uniqueness failed.");
        Console.WriteLine("PASS isolated SQL overtime payroll: EF Request writes with trigger, atomic processing, duplicate/retry protection and database-enforced status lock.");
    }
    sealed class Factory(string connection):IDbContextFactory<HrmTeContext>
    {public HrmTeContext CreateDbContext()=>new(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);}
}
