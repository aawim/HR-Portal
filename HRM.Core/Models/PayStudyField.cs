namespace HRM.Models;
public sealed class PayStudyField
{
    public int Id {get;set;}
    public int JobFamilyId {get;set;}
    public string Name {get;set;}="";
    public string Source {get;set;}="User supplied suggestions";
    public bool IsActive {get;set;}=true;
}
