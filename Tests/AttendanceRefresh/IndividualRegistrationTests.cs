using HRM.Models;
using HRM.DTOs.UserContext;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
static class IndividualRegistrationTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static readonly byte[] Png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg==");
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),x=>x.EnableNullChecks(false)).ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var allowed=true;
        var access=Proxy.For<IUserAccessService>((m,a)=>m switch{"RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=1,IndividualId=100,IsSuperAdministrator=allowed}),"GetCurrentOrganisationIdAsync"=>Task.FromResult(10),"HasOrganisationAccessAsync"=>Task.FromResult(true),"HasPermissionAsync"=>Task.FromResult(false),_=>throw new Exception(m)});
        var logs=Proxy.For<IOperationLogService>((m,a)=>m=="CreateAsync"?Audit((HrmTeContext)a[0]!):throw new Exception(m));
        await using(var db=factory.CreateDbContext()){db.Countries.AddRange(new(){CountryId=1,NameEnglish="Local"},new(){CountryId=2,NameEnglish="Foreign"});db.BusinessEntityTypes.Add(new(){BusinessEntityTypeId=1,TypeName="Individual"});await db.SaveChangesAsync();}
        var service=new IndividualRegistrationService(factory,access,logs);
        IndividualRegistrationModel Model(bool foreign=false)=>new(){FirstName="Test",LastName="Person",GenderTypeId=1,DateOfBirth=new(1990,1,1),CountryId=foreign?2:1,IsForeigner=foreign,NationalId=" a999010 ",PassportNumber=" p123456 "};
        var local=await service.RegisterAsync(Model());
        var foreignModel=Model(true);foreignModel.ImageData=Png.ToArray();var foreign=await service.RegisterAsync(foreignModel);
        await using(var db=factory.CreateDbContext())
        {
            Check(await db.Idcards.AnyAsync(c=>c.BusinessEntityId==local&&c.IdcardNumber=="A999010")&&!await db.Passports.AnyAsync(c=>c.BusinessEntityId==local),"Local identity did not use only the national ID");
            Check(await db.Passports.AnyAsync(c=>c.BusinessEntityId==foreign&&c.PassportNumber=="P123456")&&!await db.Idcards.AnyAsync(c=>c.BusinessEntityId==foreign),"Foreigner identity did not use only the passport");
            var person=await db.Individuals.SingleAsync(i=>i.BusinessEntityId==foreign);Check(person.IsForeigner==true&&person.ImageContentType=="image/png"&&person.ImageData!.SequenceEqual(Png),"Image/classification did not persist");
            Check(!await db.Users.AnyAsync()&&!await db.Jobs.AnyAsync(),"Individual registration created employment/account");
        }
        async Task Rejected(IndividualRegistrationModel model,string message){try{await service.RegisterAsync(model);throw new Exception(message);}catch(InvalidOperationException){}}
        await Rejected(Model(true),"Duplicate passport accepted");await Rejected(Model(),"Duplicate national ID accepted");
        var missing=Model(true);missing.PassportNumber=null;await Rejected(missing,"Foreign registration accepted a national ID instead of required passport");
        missing=Model();missing.NationalId=null;await Rejected(missing,"Local registration accepted passport instead of national ID");
        var bad=Model(true);bad.PassportNumber="NEW-PASSPORT";bad.ImageData=System.Text.Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>");await Rejected(bad,"Executable image accepted");
        bad.ImageData=new byte[IndividualImage.MaximumBytes+1];await Rejected(bad,"Oversize image accepted");
        bad.ImageData=Png.ToArray();System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bad.ImageData.AsSpan(16,4),5000);await Rejected(bad,"Excessive dimensions accepted");
        Check(IndividualImage.DataUrl(Png,"image/png")!.StartsWith("data:image/png;base64,")&&IndividualImage.DataUrl(Png,"image/svg+xml")==null,"Image display did not validate stored MIME type");
        allowed=false;var denied=false;try{await service.RegisterAsync(Model());}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Registration permission bypassed");
    }
    static async Task<OperationLog> Audit(HrmTeContext db){var log=new OperationLog();db.OperationLogs.Add(log);await db.SaveChangesAsync();return log;}
}
