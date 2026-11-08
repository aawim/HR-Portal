namespace HRM.DTOs.Contracts;

public sealed class JobTypeLeaveAccrualRuleDto
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
