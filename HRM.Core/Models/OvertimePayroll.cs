
namespace HRM.Models;
public sealed class OvertimePayrollBatch
{
    public long Id {get;set;}
    public int OrganisationId {get;set;}
    public DateTime PayrollMonth {get;set;}
    public string Reference {get;set;}="";
    public Guid ClientBatchId {get;set;}
    public int ProcessedByUserId {get;set;}
    public DateTime ProcessedAtUtc {get;set;}
    public int OperationLogId {get;set;}
    public ICollection<OvertimePayrollItem> Items {get;set;}=new List<OvertimePayrollItem>();
}

public sealed class OvertimePayrollItem
{
    public int RequestId {get;set;}
    public long BatchId {get;set;}
    public int IndividualId {get;set;}
    public int JobId {get;set;}
    public DateTime WorkDate {get;set;}
    public int Minutes {get;set;}
    public string RequestReference {get;set;}="";
    public OvertimePayrollBatch Batch {get;set;}=null!;
}
