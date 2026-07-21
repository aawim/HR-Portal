namespace HRM.Models.WorkPlanning
{
    public class WorkTemplate
    {
        public int WorkTemplateId { get; set; }

        public int WorkTemplateTypeId { get; set; }

        public int? OrganisationBusinessEntityId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Description { get; set; }

        public TimeOnly? DefaultStartTime { get; set; }

        public TimeOnly? DefaultEndTime { get; set; }

        public bool EndsNextDay { get; set; }

        public int DefaultGraceMinutes { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresCheckOut { get; set; }

        public bool IsRepeatable { get; set; }

        public bool IsGlobal { get; set; }

        public bool IsActive { get; set; }

        public DateOnly? EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public int? OperationLogId { get; set; }

        public virtual WorkTemplateType WorkTemplateType { get; set; } = null!;

        public virtual Organisation? Organisation { get; set; }

        public virtual OperationLog? OperationLog { get; set; }

        public virtual ICollection<WorkTemplateSegment> WorkTemplateSegments { get; set; } = new List<WorkTemplateSegment>();

        public virtual ICollection<WorkAssignment> WorkAssignments { get; set; } = new List<WorkAssignment>();
    }
}
