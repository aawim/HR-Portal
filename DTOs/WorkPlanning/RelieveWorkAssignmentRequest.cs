namespace HRM.DTOs.WorkPlanning
{
    public sealed class RelieveWorkAssignmentRequest
    {
        public long WorkAssignmentId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public DateTime RelievedDateTime { get; set; }

        public int OperationLogId { get; set; }

        public int? RelievedByUserId { get; set; }

        public string? Reason { get; set; }
    }
}
