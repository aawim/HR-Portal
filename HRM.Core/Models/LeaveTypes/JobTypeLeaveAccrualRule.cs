
namespace HRM.Models.LeaveTypes;

public sealed class JobTypeLeaveAccrualRule
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int JobTypeId { get; set; }
    public int LeaveTypeId { get; set; }
    public decimal GrantDays { get; set; }
    public int FirstGrantAfterMonths { get; set; }
    public int RepeatEveryMonths { get; set; } = 12;
    public string CarryForwardMode { get; set; } = "Unspecified";
    public decimal? CarryForwardLimit { get; set; }
    public decimal? MaximumBalance { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public bool IsEnabled { get; set; }
    public int Version { get; set; } = 1;
    public int? OperationLogId { get; set; }
}


public sealed class LeaveAccrualPosting
{
    public long Id { get; set; }
    public int RuleId { get; set; }
    public int RuleVersion { get; set; }
    public int JobId { get; set; }
    public int JobLeaveTypeId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal GrantedDays { get; set; }
    public decimal CarryAdjustment { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public DateTime PostedAtUtc { get; set; }
    public int OperationLogId { get; set; }
}
