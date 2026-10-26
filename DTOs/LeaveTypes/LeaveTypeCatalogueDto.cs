namespace HRM.DTOs.LeaveTypes
{
    public sealed class LeaveTypeCatalogueDto
    {
        public int LeaveTypeId { get; set; }

        public string Code { get; set; } = "";

        public string Name { get; set; } = "";

        public string? NameDhivehi { get; set; }

        public string? Description { get; set; }

        public int? OrganisationId { get; set; }

        public bool IsSystemType { get; set; }

        public bool IsGlobal { get; set; }

        public bool IsActive { get; set; }


    }
}
