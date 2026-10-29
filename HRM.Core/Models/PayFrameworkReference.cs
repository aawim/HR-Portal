namespace HRM.Models;
public sealed class PayJobFamily
{
    public int JobFamilyId {get;set;}
    public string Name {get;set;}="";
    public bool IsActive {get;set;}
}
public sealed class PayProfession
{
    public int Id {get;set;}
    public int JobFamilyId {get;set;}
    public string Name {get;set;}="";
    public string Tasks {get;set;}="";
    public string ExampleTitlesJson {get;set;}="[]";
    public string MisscoCodesJson {get;set;}="[]";
    public string SourceEdition {get;set;}="";
    public int SourcePage {get;set;}
    public bool IsActive {get;set;}=true;
}
