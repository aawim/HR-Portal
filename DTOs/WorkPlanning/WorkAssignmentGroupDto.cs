namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkAssignmentGroupDto
    {
        public int GroupId { get; set; }

        public int WorkPlanId { get; set; }

        public int OrganisationStructureId { get; set; }

        public string DepartmentName { get; set; } =
            string.Empty;

        public string AssignmentName { get; set; } =
            string.Empty;

        public DateTime ScheduledStart { get; set; }

        public DateTime ScheduledEnd { get; set; }

        public int RequiredStaffCount { get; set; }

        public int AssignedStaffCount { get; set; }

        public List<WorkAssignmentEmployeeDto> Employees { get; set; } =
            new();

        public string AssignmentStatus
        {
            get
            {
                if (AssignedStaffCount == 0)
                    return "Unassigned";

                if (AssignedStaffCount < RequiredStaffCount)
                    return "Partially Assigned";

                return "Fully Assigned";
            }
        }

      
    }
}
