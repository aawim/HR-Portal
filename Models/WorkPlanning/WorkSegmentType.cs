namespace HRM.Models.WorkPlanning
{
    public class WorkSegmentType
    {
        public int WorkSegmentTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public virtual ICollection<WorkTemplateSegment>
            WorkTemplateSegments
        { get; set; }
                = new List<WorkTemplateSegment>();

        public virtual ICollection<WorkAssignmentSegment>
            WorkAssignmentSegments
        { get; set; }
                = new List<WorkAssignmentSegment>();
    }
}
