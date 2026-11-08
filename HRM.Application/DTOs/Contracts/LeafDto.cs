namespace HRM.DTOs.Contracts;

public sealed class LeafDto
{
    public int LeaveId { get; set; }
    public int JobId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Duration { get; set; }
    public string? HandoverRuleSnapshot { get; set; }
    public string? ChitNo { get; set; }
    public int? LeaveTypeId { get; set; }
    public int OperationLogId { get; set; }
    public int LeaveStateId { get; set; }
    public global::HRM.DTOs.Contracts.LeaveStateDto? LeaveState { get; set; } = default!;
    public global::HRM.DTOs.Contracts.LeaveTypeDto? LeaveType { get; set; } = default!;
}
