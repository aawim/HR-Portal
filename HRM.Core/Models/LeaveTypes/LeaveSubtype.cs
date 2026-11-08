namespace HRM.Models.LeaveTypes;

public sealed class LeaveSubtype
{
    public int Id {get;set;}
    public int OrganisationId {get;set;}
    public int LeaveTypeId {get;set;}
    public string Name {get;set;}="";
    public decimal Days {get;set;}
    public string CertificateRule {get;set;}="Any";
    public bool IsActive {get;set;}=true;
    public int Version {get;set;}=1;
}

public sealed class LeaveSubtypeUsage
{
    public int LeaveId {get;set;}
    public int JobLeaveTypeId {get;set;}
    public int SubtypeId {get;set;}
    public DateTime PeriodStart {get;set;}
    public decimal DaysLimit {get;set;}
    public string SubtypeName {get;set;}="";
    public string? CertificateName {get;set;}
    public string? ContentType {get;set;}
    public byte[]? ProtectedCertificate {get;set;}
    public Leaf Leave {get;set;}=null!;
    public LeaveSubtype Subtype {get;set;}=null!;
}
