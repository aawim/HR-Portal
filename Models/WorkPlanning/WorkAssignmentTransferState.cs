namespace HRM.Models.WorkPlanning
{
    public partial class WorkAssignmentTransferState
    {
        public int WorkAssignmentTransferStateId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsProcessingState { get; set; }

        public bool IsFinalState { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<WorkAssignmentTransfer> WorkAssignmentTransfers
        { get; set; } = new List<WorkAssignmentTransfer>();
    }
}
