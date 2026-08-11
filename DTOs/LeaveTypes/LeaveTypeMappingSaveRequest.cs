namespace HRM.DTOs.LeaveTypes
{
    public class LeaveTypeMappingSaveRequest
    {
        public int LegacyLeaveTypeId { get; set; }

        public int? LeaveDefinitionId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
    }
}
