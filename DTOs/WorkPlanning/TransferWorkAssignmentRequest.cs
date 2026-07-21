namespace HRM.DTOs.WorkPlanning
{
    public sealed class TransferWorkAssignmentRequest
    {
        public long WorkAssignmentId { get; set; }

        public int FromIndividualId { get; set; }

        public int FromJobId { get; set; }

        public int ToIndividualId { get; set; }

        public int ToJobId { get; set; }

        public DateTime TransferDateTime { get; set; }

        public int OperationLogId { get; set; }

        public int? TransferredByUserId { get; set; }

        public string? Reason { get; set; }
    }
}
