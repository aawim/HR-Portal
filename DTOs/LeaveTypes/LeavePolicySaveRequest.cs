using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.LeaveTypes
{
    public sealed class LeavePolicySaveRequest
    {
        public int? LeavePolicyId { get; set; }

        [Required]
        public int LeaveDefinitionId { get; set; }

        [MaxLength(200)]
        public string? Name { get; set; }

        public decimal? DefaultEntitlementDays { get; set; }

        public bool IncludeHolidays { get; set; }

        public bool IncludePay { get; set; } = true;

        public decimal? PayPercentage { get; set; } = 100;

        public bool IsLocationRequired { get; set; }

        public bool IsStaffWideAvailable { get; set; }

        public int? MinimumServiceMonths { get; set; }

        public int RequestTypeId { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; } =
            DateTime.Today;

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
