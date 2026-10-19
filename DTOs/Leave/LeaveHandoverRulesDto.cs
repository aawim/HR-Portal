namespace HRM.DTOs.Leave;

public record LeaveHandoverRecipient(int JobId, string Name);
public record LeaveHandoverRulesDto(int? AfterDays, List<LeaveHandoverRecipient> Recipients);
public class LeaveHandoverTaskInput
{
    public int? JobId { get; set; }
    public string Tasks { get; set; } = "";
}
public class LeaveHandoverInboxItem
{
    public int LeaveId { get; set; }
    public int Revision { get; set; }
    public string Reference { get; set; } = "";
    public string Applicant { get; set; } = "";
    public string Dates { get; set; } = "";
    public string Status { get; set; } = "";
    public string? SupervisorComments { get; set; }
    public bool CanRevise { get; set; }
    public bool CanSupervise { get; set; }
    public List<LeaveHandoverTaskView> Tasks { get; set; } = [];
    public List<LeaveHandoverRecipient> Recipients { get; set; } = [];
}
public record LeaveHandoverTaskView(int Id, int JobId, string Employee, string Tasks, string Decision, string? Comments, bool CanDecide);
