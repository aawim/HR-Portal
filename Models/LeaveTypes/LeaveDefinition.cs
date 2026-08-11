namespace HRM.Models.LeaveTypes
{
    public class LeaveDefinition
    {
        public int LeaveDefinitionId { get; set; }

        public string Code { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public string? NameDhivehi { get; set; }

        public string? Description { get; set; }

        public int? OwnerOrganisationId { get; set; }

        public bool IsSystemType { get; set; }

        public bool IsGlobal { get; set; }

        public bool IsActive { get; set; }

        public int? OperationLogId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public virtual ICollection<LeavePolicy> LeavePolicies { get; set; } =
            new List<LeavePolicy>();

        public virtual ICollection<LeaveTypeMapping> LeaveTypeMappings { get; set; } =
            new List<LeaveTypeMapping>();

        public virtual ICollection<JobLeaveType> JobLeaveTypes { get; set; }
    = new List<JobLeaveType>();
    }
}
