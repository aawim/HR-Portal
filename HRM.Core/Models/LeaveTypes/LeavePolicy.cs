
namespace HRM.Models.LeaveTypes;
    public class LeavePolicy
    {
        public int LeavePolicyId { get; set; }

        public int LeaveTypeId { get; set; }

        public int OrganisationId { get; set; }

        public string? Name { get; set; }

        public decimal? DefaultEntitlementDays { get; set; }

        public bool IncludeHolidays { get; set; }

        public bool IncludePay { get; set; }

        public decimal? PayPercentage { get; set; }

        public bool RequireWorkHandover { get; set; }

        public int WorkHandoverAfterDays { get; set; } = 2;

        public bool IsLocationRequired { get; set; }

        public bool IsStaffWideAvailable { get; set; }

        public int? MinimumServiceMonths { get; set; }

        public int RequestTypeId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public int? OperationLogId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public virtual HRM.Models.LeaveType LeaveType { get; set; } =
            null!;

        public virtual ICollection<LeavePolicyAccrualRule> AccrualRules { get; set; } =
            new List<LeavePolicyAccrualRule>();
    }
