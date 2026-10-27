
namespace HRM.Models;
public sealed class VerifiedState
{
    public int VerifiedStateId {get;set;}
    public string StateName {get;set;}="";
}

public sealed class BusinessEntityState
{
    public int BusinessEntityStateId {get;set;}
    public string StateName {get;set;}="";
}
