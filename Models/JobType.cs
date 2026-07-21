using HRM.Models.Archives;
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

    public virtual ICollection<OrganisationPayrollPeriodJobType> OrganisationPayrollPeriodJobTypes { get; set; } = new List<OrganisationPayrollPeriodJobType>();

    public virtual ICollection<PayrollConfigurableValue> PayrollConfigurableValues { get; set; } = new List<PayrollConfigurableValue>();

    public virtual ICollection<PayrollItemsProcessingWhiteList> PayrollItemsProcessingWhiteLists { get; set; } = new List<PayrollItemsProcessingWhiteList>();

    public virtual ICollection<SalaryRateDefinition> SalaryRateDefinitions { get; set; } = new List<SalaryRateDefinition>();
}
