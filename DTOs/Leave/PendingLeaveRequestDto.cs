namespace HRM.DTOs.Leave;

public class PendingLeaveRequestDto
{
    public int RequestId { get; set; }
    public string Reference { get; set; } = "";
    public string Applicant { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Duration { get; set; }
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string Address { get; set; } = "";
    public string Contact { get; set; } = "";
    public List<RequestActionDto> Actions { get; set; } = new();
}
