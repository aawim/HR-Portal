namespace HRM.DTOs.LeaveTypes;

public sealed class LeaveSubtypeInput
{
    public int Id {get;set;}
    public string Name {get;set;}="";
    public decimal Days {get;set;}
    public string CertificateRule {get;set;}="Any";
    public bool IsActive {get;set;}=true;
    public int Version {get;set;}
    public bool InUse {get;set;}
}
public sealed class LeaveSubtypeSettings
{
    public int LeaveTypeId {get;set;}
    public string LeaveTypeName {get;set;}="";
    public decimal? TotalDays {get;set;}
    public List<LeaveSubtypeInput> Subtypes {get;set;}=[];
}
public sealed record LeaveSubtypeOption(int Id,string Name,decimal AllocatedDays,decimal RemainingDays,string CertificateRule);
public sealed record LeaveCertificateDownload(string Name,string ContentType,byte[] Content,string SubtypeName);
