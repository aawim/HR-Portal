namespace HRM.DTOs.Leave
{
    public class LeaveApplicationTypeDto
    {
        public int JobLeaveTypeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public int RemainingDays { get; set; }
        public bool IncludeHolidays { get; set; }
        public List<LeaveReasonDto> Reasons { get; set; } = new();
    }
}
