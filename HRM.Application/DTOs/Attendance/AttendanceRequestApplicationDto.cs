namespace HRM.DTOs.Attendance;

public class AttendanceRequestApplicationDto
{
    public int? OriginalLogId { get; set; }
    public int InOutModeId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly Time { get; set; } = TimeOnly.FromDateTime(DateTime.Now);
    public string Comments { get; set; } = string.Empty;
}

public class AttendanceRequestDateInfoDto
{
    public bool CanSubmit { get; set; } = true;
    public bool IsEarlyReturn { get; set; }
    public int? LeaveId { get; set; }
    public int BalanceAdjustment { get; set; }
    public string Message { get; set; } = string.Empty;
}
