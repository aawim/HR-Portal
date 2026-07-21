namespace HRM.Models.WorkPlanning
{
    public partial class WorkAssignmentState
    {
        public int WorkAssignmentStateId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsFinalState { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<WorkAssignment> WorkAssignments { get; set; }
            = new List<WorkAssignment>();
    }
}
