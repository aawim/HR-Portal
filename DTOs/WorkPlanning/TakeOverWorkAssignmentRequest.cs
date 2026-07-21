namespace HRM.DTOs.WorkPlanning
{
    public class TakeOverWorkAssignmentRequest
    {
        public long WorkAssignmentId { get; set; }

        public int ToIndividualId { get; set; }

        public int ToJobId { get; set; }

        public int OperationLogId { get; set; }

        public int? TakenOverByUserId { get; set; }

        public string? Reason { get; set; }
    }
}
