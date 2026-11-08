using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.Leave;

public enum LeaveChangeKind { Extension = 1, EarlyReturn = 2 }

public class LeaveChangeApplicationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an approved leave.")]
    public int LeaveId { get; set; }
    public LeaveChangeKind Kind { get; set; } = LeaveChangeKind.Extension;
    public DateTime Date { get; set; } = DateTime.Today;
    [Required, StringLength(250)] public string Reason { get; set; } = string.Empty;
}

public class LeaveChangeLeaveDto
{
    public int LeaveId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Duration { get; set; }
    public bool IncludeHolidays { get; set; }
    public bool HasPendingChange { get; set; }
}

public class LeaveChangeRequestDto
{
    public string Applicant { get; set; } = "";
    public int ChangeKind { get; set; }
    public bool IsProcessing { get; set; }
    public bool IsOwnRequest { get; set; }
    public string? ActionsMessage { get; set; }
    public string? CurrentStage { get; set; }
    public List<string> PendingApprovers { get; set; } = [];
    public int RequestId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime CurrentEndDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<RequestActionDto> Actions { get; set; } = new();
}
