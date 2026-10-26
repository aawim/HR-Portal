using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed class IndividualRegistrationModel
{
    public string FirstName {get;set;}="";public string? MiddleName {get;set;}public string? LastName {get;set;}
    public string? FirstNameDhivehi {get;set;}public string? MiddleNameDhivehi {get;set;}public string? LastNameDhivehi {get;set;}
    public int GenderTypeId {get;set;}public int CountryId {get;set;}public DateTime? DateOfBirth {get;set;}
    public string? NationalId {get;set;}public string? PassportNumber {get;set;}
    public string? AddressName {get;set;}public string? AddressLine {get;set;}public int AddressCountryId {get;set;}public int AddressBaseTypeId {get;set;}public int AddressUsageId {get;set;}
    public List<RegistrationContact> Contacts {get;set;}=[];
}
public sealed class RegistrationContact {public int TypeId {get;set;}public string Value {get;set;}="";}
public sealed record RegistrationOption(int Id,string Name);
public sealed record RegistrationOptions(List<RegistrationOption> Countries,List<RegistrationOption> AddressTypes,List<RegistrationOption> AddressUsages,List<RegistrationOption> ContactTypes);
public sealed class IndividualRegistrationService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService logs)
{
    private async Task RequireAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!(actor.IsSuperAdministrator||await access.HasPermissionAsync("Super.Admin.Manage")||await access.HasPermissionAsync("Admin")||await access.HasPermissionAsync("Individual.Add")))throw new UnauthorizedAccessException("You do not have permission to register individuals.");
    }
    public async Task<RegistrationOptions> LoadAsync()
    {
        await RequireAsync();await using var db=await factory.CreateDbContextAsync();
        return new(await db.Countries.OrderBy(x=>x.NameEnglish).Select(x=>new RegistrationOption(x.CountryId,x.NameEnglish)).ToListAsync(),await db.AddressBaseTypes.OrderBy(x=>x.TypeName).Select(x=>new RegistrationOption(x.AddressBaseTypeId,x.TypeName??"Address type")).ToListAsync(),await db.BusinessEntityLocationTypes.OrderBy(x=>x.TypeName).Select(x=>new RegistrationOption(x.BusinessEntityLocationTypeId,x.TypeName??"Address usage")).ToListAsync(),await db.ContactInformationTypes.OrderBy(x=>x.TypeName).Select(x=>new RegistrationOption(x.ContactInformationTypeId,x.TypeName??"Contact type")).ToListAsync());
    }
    public async Task<int> RegisterAsync(IndividualRegistrationModel model)
    {
        await RequireAsync();
        var names=new[]{model.FirstName,model.MiddleName,model.LastName,model.FirstNameDhivehi,model.MiddleNameDhivehi,model.LastNameDhivehi};
        if(string.IsNullOrWhiteSpace(model.FirstName)||names.Any(n=>n?.Trim().Length>250)||model.GenderTypeId is <1 or >2||!model.DateOfBirth.HasValue||model.DateOfBirth.Value.Date>DateTime.UtcNow.AddHours(5).Date||model.DateOfBirth.Value.Year<1850)throw new InvalidOperationException("Enter a first name, valid birth date and gender. Names must be 250 characters or fewer.");
        var national=model.NationalId?.Trim().ToUpperInvariant();var passport=model.PassportNumber?.Trim().ToUpperInvariant();
        if(string.IsNullOrEmpty(national)&&string.IsNullOrEmpty(passport)||national?.Length>50||passport?.Length>50)throw new InvalidOperationException("Enter a national ID or passport number of up to 50 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.Countries.AnyAsync(x=>x.CountryId==model.CountryId))throw new InvalidOperationException("Select a valid nationality.");
        if(!string.IsNullOrEmpty(national)&&await db.Idcards.AnyAsync(x=>x.IdcardNumber.Trim().ToUpper()==national)||!string.IsNullOrEmpty(passport)&&await db.Passports.AnyAsync(x=>x.CountryId==model.CountryId&&x.PassportNumber.Trim().ToUpper()==passport))throw new InvalidOperationException("This individual is already registered. Search the registry before creating another record.");
        var type=await db.BusinessEntityTypes.Where(t=>t.TypeName=="Individual").Select(t=>(int?)t.BusinessEntityTypeId).SingleOrDefaultAsync()
            ?? await db.Individuals.Select(i=>(int?)i.BusinessEntity.BusinessEntityTypeId).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Individual registration is not configured. Contact the administrator.");
        var audit=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_CREATE,"Manual individual registration; identity verification remains pending. No employee account or tenant employment created.");
        var entity=new BusinessEntity{BusinessEntityTypeId=type,OperationLogId=audit.OperationLogId};db.BusinessEntities.Add(entity);await db.SaveChangesAsync();
        var individual=new Individual{BusinessEntityId=entity.BusinessEntityID,FirstNameEnglish=model.FirstName.Trim(),MiddleNameEnglish=model.MiddleName?.Trim(),LastNameEnglish=model.LastName?.Trim(),FirstNameDhivehi=model.FirstNameDhivehi?.Trim(),MiddleNameDhivehi=model.MiddleNameDhivehi?.Trim(),LastNameDhivehi=model.LastNameDhivehi?.Trim(),DateOfBirth=model.DateOfBirth.Value.Date,GenderTypeId=model.GenderTypeId,CountryId=model.CountryId};
        db.Individuals.Add(individual);
        if(!string.IsNullOrEmpty(national))db.Idcards.Add(new(){BusinessEntity=individual,IdcardNumber=national,IdcardStateId=1,OperationLogId=audit.OperationLogId});
        if(!string.IsNullOrEmpty(passport))db.Passports.Add(new Passport{BusinessEntity=individual,PassportNumber=passport,CountryId=model.CountryId,PassportStateId=1,OperationLogId=audit.OperationLogId});
        await db.SaveChangesAsync();
        if(!string.IsNullOrWhiteSpace(model.AddressName)||!string.IsNullOrWhiteSpace(model.AddressLine))
        {
            var result=await BusinessEntityAddressWriter.SaveAsync(db,individual.BusinessEntityId,new AddressMutation{Name=model.AddressName,Line1=model.AddressLine,BaseTypeId=model.AddressBaseTypeId,UsageId=model.AddressUsageId,CountryId=model.AddressCountryId,IsCurrent=true},audit.OperationLogId);
            if(!result.Success)throw new InvalidOperationException(result.Message);
        }
        foreach(var contact in model.Contacts.Where(x=>!string.IsNullOrWhiteSpace(x.Value)))
        {
            if(contact.Value.Length>250)throw new InvalidOperationException("Contact values must be 250 characters or fewer.");
            var contactType=await db.ContactInformationTypes.SingleOrDefaultAsync(x=>x.ContactInformationTypeId==contact.TypeId)??throw new InvalidOperationException("Select a valid contact type.");
            if(!string.IsNullOrEmpty(contactType.Regex)&&!System.Text.RegularExpressions.Regex.IsMatch(contact.Value,contactType.Regex,System.Text.RegularExpressions.RegexOptions.None,TimeSpan.FromMilliseconds(250)))throw new InvalidOperationException("A contact value does not match its selected type.");
            db.BussinessEntityContactInformations.Add(new(){BussinessEntityId=individual.BusinessEntityId,ContactInformationTypeId=contact.TypeId,Value=contact.Value.Trim(),IsValid=true,OperationLogId=audit.OperationLogId});
        }
        await db.SaveChangesAsync();await tx.CommitAsync();return individual.BusinessEntityId;
    }
}
