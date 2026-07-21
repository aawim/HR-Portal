namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkAssignmentEmployeeDto
    {
        public long WorkAssignmentId { get; set; }

        public long? WorkAssignmentOwnerId { get; set; }

        public int? IndividualId { get; set; }

        public int? JobId { get; set; }

        public string EmployeeName { get; set; } =
            string.Empty;

        public string JobTitle { get; set; } =
            string.Empty;

        public bool HasCurrentOwner { get; set; }

        public string AssignmentState { get; set; } =
            string.Empty;


    }
}
