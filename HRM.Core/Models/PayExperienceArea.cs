namespace HRM.Models;
public sealed class PayExperienceArea
{
    public int Id {get;set;}
    public int? JobFamilyId {get;set;}
    public string Sector {get;set;}="";
    public string Name {get;set;}="";
    public string Description {get;set;}="";
    public string Source {get;set;}="User supplied sector examples";
    public bool IsActive {get;set;}=true;
}
