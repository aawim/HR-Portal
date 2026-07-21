namespace HRM.DTOs.WorkPlanning
{
    public sealed class BulkAssignmentResult
    {
        public bool Success { get; set; }

        public int TotalSelected { get; set; }

        public int AssignedCount { get; set; }

        public int SkippedCount { get; set; }

        public List<BulkAssignmentItemResult> Items { get; set; } = new();

        public string Message { get; set; } = string.Empty;
    }
    public sealed class BulkAssignmentItemResult
    {
        public int IndividualId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public long? WorkAssignmentId { get; set; }

        public bool Success { get; set; }

        public string? Message { get; set; }
    }
}
