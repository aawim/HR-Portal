namespace HRM.DTOs.WorkPlanning
{
    public class BulkAssignmentResultDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public int TotalSelected { get; set; }

        public int AssignedCount { get; set; }

        public int SkippedCount { get; set; }

        public List<string> Warnings { get; set; } =
            new();


    }
}
