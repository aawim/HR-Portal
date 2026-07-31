namespace HRM.DTOs.StaffSchedule
{
    public sealed class ShiftAssignmentHistoryDto
    {
        public int ShiftAssignmentId { get; set; }

        public int ShiftId { get; set; }

        public string ShiftName { get; set; } =
            string.Empty;

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }
    }
}
