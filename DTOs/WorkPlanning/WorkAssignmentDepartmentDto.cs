namespace HRM.DTOs.WorkPlanning
{
    public sealed class WorkAssignmentDepartmentDto
    {
        public int OrganisationStructureId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public string DepartmentName { get; set; } =
            string.Empty;
    }
}
