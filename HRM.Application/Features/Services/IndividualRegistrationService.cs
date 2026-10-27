using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.Services;
public sealed class IndividualRegistrationModel
{
    public string FirstName {get;set;}="";public string? MiddleName {get;set;}public string? LastName {get;set;}
    public string? FirstNameDhivehi {get;set;}public string? MiddleNameDhivehi {get;set;}public string? LastNameDhivehi {get;set;}
    public int GenderTypeId {get;set;}public int CountryId {get;set;}public DateTime? DateOfBirth {get;set;}
    public bool IsForeigner {get;set;}
    public byte[]? ImageData {get;set;}
    public string? NationalId {get;set;}public string? PassportNumber {get;set;}
    public string? AddressName {get;set;}public string? AddressLine {get;set;}public int AddressCountryId {get;set;}public int AddressBaseTypeId {get;set;}public int AddressUsageId {get;set;}
    public List<RegistrationContact> Contacts {get;set;}=[];
}

public sealed class RegistrationContact {public int TypeId {get;set;}public string Value {get;set;}="";}

public sealed record RegistrationOption(int Id,string Name);

public sealed record RegistrationOptions(List<RegistrationOption> Countries,List<RegistrationOption> AddressTypes,List<RegistrationOption> AddressUsages,List<RegistrationOption> ContactTypes);
