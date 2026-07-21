using HRM.Enum;

namespace HRM.Models.WorkPlanning
{
    public partial class WorkAssignmentTransfer
    {
        public long WorkAssignmentTransferId { get; set; }

        public long WorkAssignmentId { get; set; }

        public int WorkAssignmentTransferStateId { get; set; }

        public long FromWorkAssignmentOwnerId { get; set; }

        public long? ToWorkAssignmentOwnerId { get; set; }

        public int FromIndividualId { get; set; }

        public int FromJobId { get; set; }

        public int ToIndividualId { get; set; }

        public int ToJobId { get; set; }

        public WorkTransferType TransferType { get; set; }

        public string? Reason { get; set; }


        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public int? RequestId { get; set; }

        public DateTime RequestedDate { get; set; }

        public int? RequestedByUserId { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public int? ApprovedByUserId { get; set; }

        public DateTime? RejectedDate { get; set; }

        public int? RejectedByUserId { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool IsValid { get; set; }

        public int? OperationLogId { get; set; }

        public virtual WorkAssignment WorkAssignment { get; set; } = null!;

        public virtual WorkAssignmentTransferState WorkAssignmentTransferState { get; set; } = null!;

        public virtual WorkAssignmentOwner FromWorkAssignmentOwner { get; set; } = null!;

        public virtual WorkAssignmentOwner? ToWorkAssignmentOwner { get; set; }

        public virtual Request? Request { get; set; }

        public virtual OperationLog? OperationLog { get; set; }

        public virtual Individual FromIndividual { get; set; } = null!;

        public virtual Job FromJob { get; set; } = null!;

        public virtual Individual ToIndividual { get; set; } = null!;

        public virtual Job ToJob { get; set; } = null!;

         public virtual User? RequestedByUser { get; set; }

        public virtual User? ApprovedByUser { get; set; }

        public virtual User? RejectedByUser { get; set; }

    
    }
}
