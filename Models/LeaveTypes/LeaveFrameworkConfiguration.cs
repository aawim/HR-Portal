using HRM.Enum;

namespace HRM.Models.LeaveTypes
{
    public class LeaveFrameworkConfiguration
    {
        public int LeaveFrameworkConfigurationId { get; set; }

        public int OrganisationId { get; set; }

        public LeaveFrameworkType FrameworkType { get; set; }

        public MigrationState MigrationState { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        public int? OperationLogId { get; set; }
    }
}
