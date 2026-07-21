namespace HRM.DTOs.Leave
{
    public class AssignLeaveTypeDto
    {
        public int JobId { get; set; }

        public int LeaveTypeId { get; set; }

        public DateTime EffectiveFromDate { get; set; } = DateTime.Today;

        public DateTime? EffectiveToDate { get; set; }

        public int RemainingDays { get; set; }

        public bool IsValid { get; set; } = true;
    }
}
