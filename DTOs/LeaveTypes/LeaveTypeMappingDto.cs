namespace HRM.DTOs.LeaveTypes
{
    public class LeaveTypeMappingDto
    {
        public int LeaveTypeMappingId { get; set; }

        public int? LegacyLeaveTypeId { get; set; }

        public string LegacyLeaveTypeName { get; set; } =
            string.Empty;

        public int? LeaveDefinitionId { get; set; }

        public string LeaveDefinitionCode { get; set; } =
            string.Empty;

        public string LeaveDefinitionName { get; set; } =
            string.Empty;

        public int? OrganisationId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public bool IsCurrent =>
            IsActive &&
            (!EffectiveFrom.HasValue ||
             EffectiveFrom.Value.Date <= DateTime.Today) &&
            (!EffectiveTo.HasValue ||
             EffectiveTo.Value.Date >= DateTime.Today);
    }
}
