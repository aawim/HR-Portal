using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationPayrollPeriodJobType
{
    public int OrganisationPayrollPeriodJobTypeId { get; set; }

    public int JobTypeId { get; set; }

    public bool IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public int OrganisationPayrollPeriodId { get; set; }

    public virtual JobType JobType { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual OrganisationPayrollPeriod OrganisationPayrollPeriod { get; set; } = null!;
}
