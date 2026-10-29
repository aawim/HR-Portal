namespace HRM.Models;
public sealed class ProfessionRequirementTemplate
{
    public int Id {get;set;}
    public int ProfessionId {get;set;}
    public int PublicServiceRank {get;set;}
    public string ProfileJson {get;set;}="{}";
    public string SourceReference {get;set;}="";
    public bool IsActive {get;set;}=true;
}
