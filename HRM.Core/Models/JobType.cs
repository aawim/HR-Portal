using HRM.Models.Archives;
using HRM.Models.Workflow;
using System;
using System.Collections.Generic;
namespace HRM.Models;

public partial class JobType
{
    public int JobTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? TypeNameDhivehi { get; set; }

    public int? CscjobTypePrimaryKey { get; set; }

    public bool? IsActive { get; set; }

    public int? OperationLogId { get; set; }

    public virtual ICollection<JobTypesException> JobTypesExceptions { get; set; } = new List<JobTypesException>();

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual ICollection<LeaveConfigurableValue> LeaveConfigurableValues { get; set; } = new List<LeaveConfigurableValue>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<SalaryRateDefinition> SalaryRateDefinitions { get; set; } = new List<SalaryRateDefinition>();

    //public virtual ICollection<WfWorkflowDefinition> WorkflowDefinitions { get; set; } = new List<WfWorkflowDefinition>();
}
