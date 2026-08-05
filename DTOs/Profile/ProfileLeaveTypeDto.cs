namespace HRM.DTOs.Profile
{
    public class ProfileLeaveTypeDto
    {
        public int JobLeaveTypeId { get; set; }

        public int JobId { get; set; }

        public int LeaveTypeId { get; set; }

        public string LeaveTypeName { get; set; } =
            string.Empty;

        public string? LeaveTypeCode { get; set; }

        public decimal AllocatedDays { get; set; }

        public decimal UsedDays { get; set; }

        public decimal RemainingDays { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public DateTime? LastLeaveTakenDate { get; set; }

        public DateTime? RenewedDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsValid { get; set; }

        public string EffectivePeriodText { get; set; } =
            string.Empty;
    }
}
