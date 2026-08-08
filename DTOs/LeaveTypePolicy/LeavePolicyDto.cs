namespace HRM.DTOs.LeaveTypePolicy
{
    public sealed class LeavePolicyDto
    {
        public int LeavePolicyId { get; set; }

        public int LeaveDefinitionId { get; set; }

        public string LeaveTypeCode { get; set; } =
            string.Empty;

        public string LeaveTypeName { get; set; } =
            string.Empty;

        public int OrganisationId { get; set; }

        public string? Name { get; set; }

        public decimal? DefaultEntitlementDays { get; set; }

        public bool IncludeHolidays { get; set; }

        public bool IncludePay { get; set; }

        public decimal? PayPercentage { get; set; }

        public bool IsLocationRequired { get; set; }

        public bool IsStaffWideAvailable { get; set; }

        public int? MinimumServiceMonths { get; set; }

        public int? RequestTypeId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public bool IsCurrentlyEffective =>
            IsActive &&
            EffectiveFrom.Date <= DateTime.Today &&
            (
                !EffectiveTo.HasValue ||
                EffectiveTo.Value.Date >= DateTime.Today
            );
    }
}
