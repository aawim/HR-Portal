namespace HRM.Models.LeaveTypes
{
    public class LeaveTypeMapping
    {
        public int LeaveTypeMappingId { get; set; }

        public int LegacyLeaveTypeId { get; set; }

        public int LeaveDefinitionId { get; set; }

        public int? OrganisationId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public int? OperationLogId { get; set; }

        public virtual LeaveDefinition LeaveDefinition { get; set; } =
            null!;
    }
}
