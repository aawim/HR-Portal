namespace HRM.DTOs.LeaveTypes
{
    public sealed class LeaveDefinitionDto
    {
        public int LeaveDefinitionId { get; set; }

        public string Code { get; set; } = "";

        public string Name { get; set; } = "";

        public string? NameDhivehi { get; set; }

        public string? Description { get; set; }

        public int? OwnerOrganisationId { get; set; }

        public bool IsSystemType { get; set; }

        public bool IsGlobal { get; set; }

        public bool IsActive { get; set; }

        public bool IsLegacyMapped { get; set; }

        public int? LegacyLeaveTypeId { get; set; }
    }
}
