namespace HRM.DTOs.Leave
{
    public class ProcessingLeaveDto
    {
        public int? LeaveTypeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? Duration { get; set; }
        public string? StateName { get; set; } = "";
    }
}
