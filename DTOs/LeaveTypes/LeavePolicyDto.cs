namespace HRM.DTOs.LeaveTypes
{
    public sealed class LeavePolicyDto
    {
        public int LeavePolicyId { get; set; }

        public int LeaveDefinitionId { get; set; }
         public  int RequestTypeId { get; set; }
        public string LeaveTypeName { get; set; } = "";

        public int OrganisationId { get; set; }

        public string? LeaveTypeCode { get; set; }
        public string? Name { get; set; }

        public decimal? DefaultEntitlementDays { get; set; }

        public bool IncludeHolidays { get; set; }

        public bool IncludePay { get; set; }

        public decimal? PayPercentage { get; set; }

        public bool IsLocationRequired { get; set; }

        public bool IsStaffWideAvailable { get; set; }

        public int? MinimumServiceMonths { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }
    }
}
