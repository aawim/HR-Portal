namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkAssignmentTransferStateDto
    {
        public long WorkAssignmentId { get; set; }

        public long? WorkAssignmentTransferId { get; set; }

        public int? TransferStateId { get; set; }

        public string StateName { get; set; } = "No Transfer";

        public bool HasTransfer { get; set; }
    }
}
