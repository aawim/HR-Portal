using HRM.Enum;

namespace HRM.Models.WorkPlanning
{
    public partial class WorkAssignmentOwner
    {
        public long WorkAssignmentOwnerId { get; set; }

        public long WorkAssignmentId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public WorkOwnershipType OwnershipType { get; set; }

        public DateTime AssignedDate { get; set; }

        public int? AssignedByUserId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public DateTime? RelievedDate { get; set; }

        public int? RelievedByUserId { get; set; }

        public string? ReliefReason { get; set; }

        public bool IsCurrentOwner { get; set; }

        public bool IsValid { get; set; }

        public int? OperationLogId { get; set; }

        public virtual WorkAssignment WorkAssignment { get; set; } = null!;

        public virtual Individual Individual { get; set; } = null!;

        public virtual Job Job { get; set; } = null!;

        public virtual User? AssignedByUser { get; set; }

        public virtual User? RelievedByUser { get; set; }

        public virtual OperationLog? OperationLog { get; set; }
    }
}
