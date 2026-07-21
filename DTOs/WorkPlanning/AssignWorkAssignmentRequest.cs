namespace HRM.DTOs.WorkPlanning
{
    public sealed class AssignWorkAssignmentRequest
    {
        public long WorkAssignmentId { get; set; }

        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public int OperationLogId { get; set; }

        public int? AssignedByUserId { get; set; }

        public string? Remarks { get; set; }
    }
}
