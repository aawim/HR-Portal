using HRM.Models.Archives;
namespace HRM.Models;
public sealed class JobTypeLeaveSet
{
    public int Id {get;set;}
    public int OrganisationId {get;set;}
    public int JobTypeId {get;set;}
    public int LeaveSetId {get;set;}
    public bool IsActive {get;set;}=true;
    public int OperationLogId {get;set;}
}
