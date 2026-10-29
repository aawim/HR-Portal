namespace HRM.Models;

/// <summary>Recovery evidence for explicitly retired legacy database tables.</summary>
public sealed class TableRetirementAudit
{
    public long Id {get;set;}
    public string MigrationId {get;set;}="";
    public string OriginalTableName {get;set;}="";
    public long RecordCount {get;set;}
    public string SchemaJson {get;set;}="";
    public string DataJson {get;set;}="[]";
    public DateTime ArchivedAtUtc {get;set;}
    public string Reason {get;set;}="";
}
