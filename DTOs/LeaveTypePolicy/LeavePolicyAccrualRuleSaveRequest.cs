using HRM.Enum;
using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.LeaveTypePolicy
{
    public sealed class LeavePolicyAccrualRuleSaveRequest
    {
        public int? LeavePolicyAccrualRuleId { get; set; }

        [Required]
        public int LeavePolicyId { get; set; }

        [Required]
        public LeaveAccrualType AccrualType { get; set; }

        public decimal? AccrualAmount { get; set; }

        public int? AccrualIntervalMonths { get; set; }

        public int? AccrualStartMonth { get; set; }

        public decimal? MaximumBalance { get; set; }

        public bool CarryForwardAllowed { get; set; }

        public decimal? MaximumCarryForward { get; set; }

        public int? ExpiresAfterMonths { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
