
namespace HRM.Models;
public sealed class OvertimeRequest
{
    public int RequestId {get;set;}public int OrganisationId {get;set;}public int IndividualId {get;set;}public int JobId {get;set;}
    public DateTime WorkDate {get;set;}public DateTime WeekStart {get;set;}public int Minutes {get;set;}
    public string Reason {get;set;}="";public string? ApprovedPolicyJson {get;set;}
    public DateTime CreatedAtUtc {get;set;}public DateTime? PaidAtUtc {get;set;}public int? PaidByUserId {get;set;}public string? PaymentReference {get;set;}
    public int? StartLogId { get; set; }
    public int? EndLogId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? PreapprovalTaskId { get; set; }
    public string ClaimSource { get; set; } = "Legacy";
    public Request Request {get;set;}=null!;
}
