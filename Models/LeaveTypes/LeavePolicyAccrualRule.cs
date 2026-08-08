using HRM.Enum;

namespace HRM.Models.LeaveTypes
{
    public class LeavePolicyAccrualRule
    {
        public int LeavePolicyAccrualRuleId { get; set; }

        public int LeavePolicyId { get; set; }

        public LeaveAccrualType AccrualType { get; set; }

        public decimal? AccrualAmount { get; set; }

        public int? AccrualIntervalMonths { get; set; }

        public int? AccrualStartMonth { get; set; }

        public decimal? MaximumBalance { get; set; }

        public bool CarryForwardAllowed { get; set; }

        public decimal? MaximumCarryForward { get; set; }

        public int? ExpiresAfterMonths { get; set; }

        public bool IsActive { get; set; }

        public int? OperationLogId { get; set; }

        public virtual LeavePolicy LeavePolicy { get; set; } =
            null!;
    }
}
