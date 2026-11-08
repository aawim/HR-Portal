namespace HRM.DTOs.Contracts;

public sealed class LeaveCarryForwardDto
{
    public long Id { get; set; }
    public int RuleId { get; set; }
    public int RuleVersion { get; set; }
    public int JobId { get; set; }
    public int JobLeaveTypeId { get; set; }
    public DateTime RenewalDate { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal CarriedDays { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal? ClosingGrant { get; set; }
    public decimal? RenewalGrant { get; set; }
    public decimal? NewBalance { get; set; }
    public int ProcessedByUserId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    public string Reason { get; set; } = "";
    public int OperationLogId { get; set; }
}
