using System.Reflection;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
internal static class IndividualImageSqlSmoke
{
    public static async Task RunAsync(HrmTeContext db)
    {
        var connection=db.Database.GetConnectionString()!;
        if(!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("HRM_LeaveUpgrade_",StringComparison.Ordinal))throw new InvalidOperationException("Image smoke requires an isolated restored database.");
        var org=await db.Organisations.Select(o=>o.BusinessEntityID).FirstAsync();var country=await db.Countries.Select(c=>c.CountryId).FirstAsync();var log=await db.OperationLogs.FirstAsync();
        var access=DispatchProxy.Create<IUserAccessService,ChatSmokeAccess>();
        ((ChatSmokeAccess)(object)access).Handler=(m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(new UserContextDto{IsSuperAdministrator=true}),"GetCurrentOrganisationIdAsync"=>Task.FromResult(org),"HasOrganisationAccessAsync"=>Task.FromResult(true),_=>throw new InvalidOperationException(m)};
        var logs=DispatchProxy.Create<IOperationLogService,ChatSmokeAccess>();((ChatSmokeAccess)(object)logs).Handler=(m,a)=>m=="CreateAsync"?Task.FromResult(log):throw new InvalidOperationException(m);
        var service=new IndividualRegistrationService(new Factory(connection),access,logs);
        var image=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg==");
        var token=Guid.NewGuid().ToString("N");
        var model=new IndividualRegistrationModel{FirstName="Isolated registration",GenderTypeId=1,DateOfBirth=new(1990,1,1),CountryId=country,IsForeigner=true,PassportNumber="SMOKE-"+token,ImageData=image};
        var id=await service.RegisterAsync(model);
        var stored=await db.Individuals.AsNoTracking().SingleAsync(i=>i.BusinessEntityId==id);
        if(stored.IsForeigner!=true||stored.ImageContentType!="image/png"||!stored.ImageData!.SequenceEqual(image)||!await db.Passports.AnyAsync(p=>p.BusinessEntityId==id)||await db.Idcards.AnyAsync(p=>p.BusinessEntityId==id)||await db.Users.AnyAsync(u=>u.BusinessEntityID==id)||await db.Jobs.AnyAsync(j=>j.IndividualID==id))throw new InvalidOperationException("SQL foreign identity/image registration failed.");
        var denied=false;try{await service.RegisterAsync(model);}catch(InvalidOperationException){denied=true;}if(!denied)throw new InvalidOperationException("SQL passport duplicate accepted.");
        model.IsForeigner=false;model.NationalId="SMOKE-LOCAL-"+token;model.ImageData=null;var local=await service.RegisterAsync(model);
        if(!await db.Idcards.AnyAsync(p=>p.BusinessEntityId==local)||await db.Passports.AnyAsync(p=>p.BusinessEntityId==local))throw new InvalidOperationException("SQL local identity selection failed.");
        denied=false;try{await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.BE_Individuals SET ImageContentType='image/svg+xml' WHERE BusinessEntityID={id}");}catch(SqlException ex)when(ex.Number==547){denied=true;}if(!denied)throw new InvalidOperationException("SQL image MIME constraint failed.");
        Console.WriteLine("PASS isolated SQL individual registration: preserved old identities, local/foreign selection, image bytes/MIME, duplicate passport denial and image constraint.");
    }
    sealed class Factory(string connection):IDbContextFactory<HrmTeContext>
    {public HrmTeContext CreateDbContext()=>new(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);}
}
