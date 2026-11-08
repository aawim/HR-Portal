
namespace HRM.Models.LeaveTypes;

public class LeaveHandoverReview
{
    public int LeaveId { get; set; }
    public int Revision { get; set; } = 1;
    public string SupervisorDecision { get; set; } = "Pending";
    public int? SupervisorApprovedByIndividualId { get; set; }
    public string? Comments { get; set; }
    public DateTime? DecisionAt { get; set; }
    public int? OperationLogId { get; set; }
}

public class LeaveHandoverSupervisor
{
    public int LeaveId { get; set; }
    public int IndividualId { get; set; }
}
