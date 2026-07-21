namespace HRM.DTOs.Leave
{
    public class JobLeaveTypeEditDto
    {
        public int JobLeaveTypeId { get; set; }

        public int RemainingDays { get; set; }

        public DateTime? EffectiveFromDate { get; set; }

        public DateTime? EffectiveToDate { get; set; }

        public bool IsLeaveInfoUpdated { get; set; }
        public bool IsValid { get; set; }

        public string? Remark { get; set; }
    }
}
