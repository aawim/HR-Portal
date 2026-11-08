using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.Leave;

public class MyLeaveApplicationDto
{
    public int RequestId { get; set; }
    public int LeaveId { get; set; }
    public bool HasHandover { get; set; }
    public string Reference { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Duration { get; set; }
    public string Status { get; set; } = "";
    public string? ReturnReason { get; set; }
    public bool CanEdit { get; set; }
    public string Reason { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string Address { get; set; } = "";
    public string? ContactNumber { get; set; }
}

public class ReturnedLeaveApplicationDto
{
    public int RequestId { get; set; }
    public DateTime ReturnedAt { get; set; }
    public string Reference { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public string ReturnReason { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Select a leave reason.")]
    public int ReasonId { get; set; }
    [MaxLength(250)] public string Explanation { get; set; } = "";
    public int? CountryId { get; set; }
    public int? IslandId { get; set; }
    [MaxLength(250)] public string Address { get; set; } = "";
    [MaxLength(250)] public string ContactNumber { get; set; } = "";
    public bool IncludeHolidays { get; set; }
    public List<LeaveReasonDto> Reasons { get; set; } = [];
    public List<LeaveHandoverTaskInput> HandoverTasks { get; set; } = [];
    public List<LeaveHandoverRecipient> Recipients { get; set; } = [];
}
